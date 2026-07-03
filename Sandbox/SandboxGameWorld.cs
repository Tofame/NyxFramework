using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Silk.NET.Input;
using NyxRender;
using NyxDrawer;
using NyxDrawer.Creatures;
using NyxDrawer.Appearance;
using NyxAssets.Client;
using NyxAssets.Sprites;
using NyxAssets.Things;
using NyxGui;
using Sandbox.Spells;
using Sandbox.Items;
using NyxNetwork.Core;
using NyxNetwork.Messaging;
using Sandbox.Networking.Packets;
using Sandbox.Networking;

namespace Sandbox;

internal class SandboxGameWorld : IDisposable
{
	private float _expGainTimer = 0f;
	private readonly CreatureDrawState _creatureDrawState = new();
	private readonly Action<Position, float, float, int> _drawCreaturesAtTile;

	private readonly SandboxProtocolGame _protocol;
	public SandboxProtocolGame Protocol => _protocol;

	public bool IsNetworkClient => _protocol.IsClient;
	public bool IsNetworkHost => _protocol.IsHost;
	public bool IsNetworkActive => _protocol.IsActive;
	public int HostPort => _protocol.HostPort;
	public string ClientId => _protocol.ClientId;
	private readonly List<RemotePlayer> _remotePlayers = new();
	public List<RemotePlayer> RemotePlayers => _remotePlayers;

	public ClientAssetBundle? ClientAssets { get; private set; }
	public AssetDrawer? Drawer { get; private set; }
	public Player? Player { get; private set; }
	public List<Npc>? Npcs { get; private set; }
	public NyxGameMap.GameMap? Map { get; private set; }
	public SpellCatalog? SpellCatalog { get; private set; }
	public ActiveSpellEffects ActiveSpellEffects { get; } = new();
	public ActiveMissileEffects ActiveMissileEffects { get; } = new();

	public event Action<int>? ExpGained;

	public SandboxGameWorld()
	{
		_protocol = new SandboxProtocolGame(this);
		_drawCreaturesAtTile = (pos, sx, sy, elevPx) =>
			_creatureDrawState.DrawCreatures(pos, sx, sy, elevPx);
	}

	public bool LoadOffThread(SandboxConfig config, AppContextInfo contextInfo, Action<string>? onProgress = null)
	{
		try
		{
			onProgress?.Invoke("Resolving client assets...");
			var thingsJsonPath = SandboxResources.TryGetThingsPath();
			var resolvedAssetsPath = SandboxResources.TryGetAssetsPath("Nyx.assets");
			var resolvedSprPath = SandboxResources.TryGetAssetsPath("Nyx.spr");
			var resolvedDatPath = SandboxResources.TryGetAssetsPath("Nyx.dat");

			bool hasLocalResources = thingsJsonPath is not null && (resolvedAssetsPath is not null || resolvedSprPath is not null);

			var readOptions = config.Client.ToReadOptions();

			if (config.Client.ExportJson)
			{
				return TryExportJson(config, readOptions);
			}

			if (!hasLocalResources)
			{
				Console.WriteLine("Make sure things.json and Nyx.assets/Nyx.spr are in resources or resources/assets.");
				return false;
			}

			bool useJsonCatalog = false;
			var metadataFormat = config.Client.MetadataFormat?.ToLowerInvariant() ?? "auto";
			if (metadataFormat == "json")
			{
				useJsonCatalog = true;
			}
			else if (metadataFormat == "dat")
			{
				useJsonCatalog = false;
			}
			else // "auto"
			{
				useJsonCatalog = thingsJsonPath is not null;
			}

			if (useJsonCatalog)
			{
				if (thingsJsonPath is null || !File.Exists(thingsJsonPath))
				{
					Console.WriteLine("Requested metadata format 'json', but things.json was not found in resources/ or resources/assets/.");
					return false;
				}
			}
			else
			{
				if (resolvedDatPath is null || !File.Exists(resolvedDatPath))
				{
					Console.WriteLine("Client metadata file not found. Expected Nyx.dat in resources/ or resources/assets/.");
					return false;
				}
			}

			if (!TryResolveSpriteSource(config, readOptions, resolvedAssetsPath, resolvedSprPath, onProgress, out var sprSource, out var usingAssetsFormat) || sprSource is null)
			{
				return false;
			}

			onProgress?.Invoke("Loading thing catalog...");
			if (!TryLoadThingCatalog(readOptions, useJsonCatalog, thingsJsonPath, resolvedDatPath, sprSource, out var bundle) || bundle is null)
			{
				sprSource.Dispose();
				return false;
			}
			ClientAssets = bundle;

			onProgress?.Invoke("Initializing items manager...");
			ItemsManager.Initialize(ClientAssets);

			var things = ClientAssets.Things;
			var spr = ClientAssets.Sprites;
			var sprFormatName = usingAssetsFormat ? "Nyx.assets" : "Nyx.spr";
			if (useJsonCatalog)
			{
				Console.WriteLine($"Loaded things.json + {sprFormatName}");
			}
			else
			{
				Console.WriteLine($"Loaded Nyx.dat / {sprFormatName} ({things.DatFormat}, dat signature {things.DatSignature})");
			}
			Console.WriteLine($"  Items …{things.ItemCount}, outfits …{things.OutfitCount}, effects …{things.EffectCount}, missiles …{things.MissileCount}");
			Console.WriteLine($"  Sprites in archive: {spr.SpriteCount}");

			onProgress?.Invoke("Generating map grid...");
			var mapCfg = config.Map;

			// Initialize the GameMap and set the sectors directory.
			Map = new NyxGameMap.GameMap();
			Map.SectorsDirectory = Path.Combine(AppContext.BaseDirectory, mapCfg.SectorsPath);

			if (!InitializeEntities(config, things, out var player, out var npcs) || player is null || npcs is null)
			{
				ClientAssets.Dispose();
				ClientAssets = null;
				return false;
			}

			Player = player;
			Npcs = npcs;

			if (Map.IsInside(Player.Position))
			{
				Map.GetTile(Player.Position).AddCreature(Player);
			}

			foreach (var npc in Npcs)
			{
				if (Map.IsInside(npc.Position))
				{
					Map.GetTile(npc.Position).AddCreature(npc);
				}
			}

			ApplyDemoPlayerStartingItems(Player);
			if (thingsJsonPath is not null)
				Console.WriteLine($"  Items: definitions from \"{thingsJsonPath}\".");
			foreach (var (slot, stack) in Player.Equipment.EnumerateEquipped())
			{
				if (stack.IsEmpty)
					continue;

				var t = stack.GetItemType();
				var countNote = stack.Count > 1 ? $" ×{stack.Count}" : "";
				Console.WriteLine($"    {slot}: {t.GetDisplayLabel()}{countNote} (atk {t.Attack}, arm {t.Armor}, {t.Weight:0.##} oz)");
			}

			onProgress?.Invoke("Loading spells catalog...");
			var spellsDir = Path.Combine(AppContext.BaseDirectory, "Spells");
			LoadSpells(spellsDir);

			var p = config.Player.Appearance;
			Console.WriteLine($"Config \"{contextInfo.ConfigPath}\"");
			Console.WriteLine($"  Map: sector-based (sectors from \"{mapCfg.SectorsPath}\")");
			var mountNote = p.HasMount ? $" lookMount={p.LookMount}" : "";
			Console.WriteLine($"  Player lookType={p.LookType} head/body/legs/feet={p.LookHead}/{p.LookBody}/{p.LookLegs}/{p.LookFeet} addons={p.LookAddons}{mountNote}");
			var npcCfgs = config.Npcs;
			for (var i = 0; i < npcCfgs.Count; i++)
			{
				var c = npcCfgs[i];
				var n = c.Appearance;
				var nx = c.TileX >= 0 ? c.TileX : 453 + i * 2;
				var ny = c.TileY >= 0 ? c.TileY : 446;
				var sh = string.IsNullOrEmpty(n.Shader) ? "(default)" : n.EffectiveShader;
				var npcMountNote = n.HasMount ? $" lookMount={n.LookMount}" : "";
				Console.WriteLine($"  NPC #{i + 1} lookType={n.LookType}{npcMountNote} at ({nx},{ny}) dir={c.Direction} shader={sh}");
			}
			Console.WriteLine("  WASD = walk tiles.  F1/F2/F3 = cast spells (F3 = aim with mouse)." + (p.HasMount ? "  R = toggle mount." : "") + "  U = engine stats.  G = player stats.  E = EXP analyzer.  B = bestiary.  Q = quest log.  I = inventory.  O = object fit.");

			onProgress?.Invoke("Loading complete!");
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Nyx async load failed: {ex.Message}");
			Cleanup();
			return false;
		}
	}

	private bool TryExportJson(SandboxConfig config, NyxAssets.Things.ClientDataReadOptions readOptions)
	{
		var datPath = SandboxResources.TryGetAssetsPath("Nyx.dat");
		var sprPath = SandboxResources.TryGetAssetsPath("Nyx.spr");

		if (datPath is null || sprPath is null)
		{
			Console.WriteLine("ExportJson requires Nyx.dat and Nyx.spr to be present in resources/ or resources/assets/.");
			return false;
		}

		var catalog = ThingCatalog.Load(File.ReadAllBytes(datPath), readOptions);
		var assetsDir = SandboxResources.AssetsDirectory;
		if (!Directory.Exists(assetsDir))
		{
			Directory.CreateDirectory(assetsDir);
		}
		var outputPath = Path.Combine(assetsDir, "things.json");
		var itemsXmlPath = SandboxResources.TryGetAssetsPath("items.xml");
		if (itemsXmlPath is not null)
		{
			Console.WriteLine($"Loading items.xml from {itemsXmlPath}...");
		}
		catalog.ExportJson(outputPath, readOptions, itemsXmlPath: itemsXmlPath);
		Console.WriteLine($"Exported {catalog.ItemCount} items, {catalog.OutfitCount} outfits, {catalog.EffectCount} effects, {catalog.MissileCount} missiles to {outputPath}");

		var assetsOutputPath = Path.Combine(assetsDir, "Nyx.assets");
		Console.WriteLine($"Converting sprite archive to custom assets: {sprPath} -> {assetsOutputPath}...");
		try
		{
			AssetArchiveWriter.ConvertSprToAssets(
				sprPath,
				assetsOutputPath,
				readOptions.ExtendedSpriteIds ?? false,
				readOptions.TransparentSprites);
			Console.WriteLine("Sprite archive conversion completed successfully.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to convert sprite archive: {ex.Message}");
		}

		Environment.Exit(0);
		return true;
	}

	private bool TryResolveSpriteSource(
		SandboxConfig config,
		NyxAssets.Things.ClientDataReadOptions readOptions,
		string? resolvedAssetsPath,
		string? resolvedSprPath,
		Action<string>? onProgress,
		out ISpriteSource? sprSource,
		out bool usingAssetsFormat)
	{
		sprSource = null;
		usingAssetsFormat = false;

		var spritesFormat = config.Client.SpritesFormat?.ToLowerInvariant() ?? "auto";
		string? selectedSprPath = null;

		if (spritesFormat == "assets")
		{
			selectedSprPath = resolvedAssetsPath;
			if (selectedSprPath is null || !File.Exists(selectedSprPath))
			{
				Console.WriteLine("Error: Explicitly requested 'assets' sprite format, but Nyx.assets was not found.");
				return false;
			}
			usingAssetsFormat = true;
		}
		else if (spritesFormat == "spr")
		{
			selectedSprPath = resolvedSprPath;
			if (selectedSprPath is null || !File.Exists(selectedSprPath))
			{
				Console.WriteLine("Error: Explicitly requested 'spr' sprite format, but Nyx.spr was not found.");
				return false;
			}
			usingAssetsFormat = false;
		}
		else // "auto"
		{
			if (resolvedAssetsPath is not null && File.Exists(resolvedAssetsPath))
			{
				selectedSprPath = resolvedAssetsPath;
				usingAssetsFormat = true;
			}
			else
			{
				selectedSprPath = resolvedSprPath;
				if (selectedSprPath is null || !File.Exists(selectedSprPath))
				{
					Console.WriteLine("Sprite archive file not found. Expected Nyx.spr or Nyx.assets.");
					return false;
				}
				usingAssetsFormat = false;
			}
		}

		if (usingAssetsFormat)
		{
			onProgress?.Invoke($"Loading assets archive {Path.GetFileName(selectedSprPath)}...");
			Console.WriteLine($"Loading assets format: \"{selectedSprPath}\"");
			AssetArchive archive;
			if (config.Client.InMemoryLoading)
			{
				Console.WriteLine("  Preloading all ZSTD assets into memory...");
				onProgress?.Invoke("Preloading all ZSTD assets into memory...");
				var bytes = File.ReadAllBytes(selectedSprPath);
				archive = AssetArchive.Load(bytes, preloadPages: true);
			}
			else
			{
				archive = AssetArchive.OpenReadOnlyFile(selectedSprPath);
			}
			archive.SetMaxCachedPages(config.Client.MaxCachedPages);
			sprSource = archive;
		}
		else
		{
			onProgress?.Invoke($"Loading spr archive {Path.GetFileName(selectedSprPath)}...");
			Console.WriteLine($"Loading spr format: \"{selectedSprPath}\"");
			if (config.Client.InMemoryLoading)
			{
				Console.WriteLine("  Preloading all SPR sprites into memory...");
				onProgress?.Invoke("Preloading all SPR sprites into memory...");
				var bytes = File.ReadAllBytes(selectedSprPath);
				sprSource = SpriteArchive.Load(bytes, readOptions, preloadSprites: true);
			}
			else
			{
				sprSource = SpriteArchive.OpenReadOnlyFile(selectedSprPath, readOptions);
			}
		}

		return true;
	}

	private bool TryLoadThingCatalog(
		NyxAssets.Things.ClientDataReadOptions readOptions,
		bool useJsonCatalog,
		string? thingsJsonPath,
		string? resolvedDatPath,
		ISpriteSource sprSource,
		out ClientAssetBundle? bundle)
	{
		bundle = null;
		try
		{
			if (useJsonCatalog)
			{
				var jsonCatalog = ThingCatalog.LoadJson(thingsJsonPath!, readOptions);
				bundle = new ClientAssetBundle(jsonCatalog, sprSource, disposeSprites: true);
			}
			else
			{
				var datBytes = File.ReadAllBytes(resolvedDatPath!);
				var rawCatalog = ThingCatalog.Load(datBytes, readOptions);
				bundle = new ClientAssetBundle(rawCatalog, sprSource, disposeSprites: true);
			}
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to load thing catalog: {ex.Message}");
			return false;
		}
	}

	private bool InitializeEntities(
		SandboxConfig config,
		ThingCatalog things,
		out Player? player,
		out List<Npc>? npcs)
	{
		player = null;
		npcs = null;

		var playerCfg = config.Player;
		var npcCfgs = config.Npcs;

		if (things.TryGetOutfit(playerCfg.Appearance.LookType) is not { } playerOutfitThing)
		{
			Console.WriteLine("Missing player outfit lookType from config — check config.toml and client data.");
			return false;
		}

		var npcThings = new List<ThingType>();
		var npcMountThings = new List<ThingType?>();
		for (var i = 0; i < npcCfgs.Count; i++)
		{
			var npcAppearance = npcCfgs[i].Appearance;
			var look = npcAppearance.LookType;
			if (things.TryGetOutfit(look) is not { } ot)
			{
				Console.WriteLine($"Missing outfit lookType {look} for NPC #{i + 1} — check config.toml and client data.");
				return false;
			}

			npcThings.Add(ot);

			ThingType? npcMount = null;
			if (npcAppearance.HasMount)
			{
				if (things.TryGetOutfit(npcAppearance.LookMount) is { } mountThing)
					npcMount = mountThing;
				else
					Console.WriteLine($"  Warning: lookMount={npcAppearance.LookMount} not found for NPC #{i + 1} — mount disabled.");
			}

			npcMountThings.Add(npcMount);
		}

		ThingType? playerMountThing = null;
		if (playerCfg.Appearance.HasMount)
		{
			if (things.TryGetOutfit(playerCfg.Appearance.LookMount) is { } mountThing)
				playerMountThing = mountThing;
			else
				Console.WriteLine($"  Warning: lookMount={playerCfg.Appearance.LookMount} not found in client data — mount disabled.");
		}

		var playerX = playerCfg.TileX >= 0 ? playerCfg.TileX : 450;
		var playerY = playerCfg.TileY >= 0 ? playerCfg.TileY : 450;

		var npcList = new List<Npc>();
		for (var i = 0; i < npcCfgs.Count; i++)
		{
			var npcCfg = npcCfgs[i];
			var npcX = npcCfg.TileX >= 0 ? npcCfg.TileX : 453 + i * 2;
			var npcY = npcCfg.TileY >= 0 ? npcCfg.TileY : 446;
			npcList.Add(new Npc(npcX, npcY, npcCfg.TileZ, npcCfg.Appearance, npcThings[i], npcMountThings[i], npcCfg.Direction));
		}

		player = new Player(playerCfg.Name, playerX, playerY, playerCfg.TileZ, playerCfg.Appearance, playerOutfitThing, playerMountThing);
		npcs = npcList;
		return true;
	}

	private void LoadSpells(string spellsDir)
	{
		try
		{
			SpellCatalog = SpellCatalog.Load(spellsDir);
			Console.WriteLine($"  Spells: {SpellCatalog.Spells.Count} definition(s), {SpellCatalog.Scripts.Count} script(s) from \"{spellsDir}\"");
			if (SpellCatalog.Spells.Count > 0)
				Console.WriteLine($"    F1 = \"{SpellCatalog.Spells[0].Name}\"");
			if (SpellCatalog.Spells.Count > 1)
				Console.WriteLine($"    F2 = \"{SpellCatalog.Spells[1].Name}\"");
			if (SpellCatalog.Spells.Count > 2)
				Console.WriteLine($"    F3 = \"{SpellCatalog.Spells[2].Name}\" (mouse target)");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"  Spells: failed to load — {ex.Message}");
			SpellCatalog = null;
		}
	}

	public bool LoadMainThread(SpriteRenderer renderer, SandboxConfig config)
	{
		try
		{
			if (ClientAssets is null || Map is null)
				return false;

			Drawer = new AssetDrawer(ClientAssets, renderer);

			var mapCfg = config.Map;



			var stats = renderer.GetStats();
			Console.WriteLine($"Sprites in atlas: {stats.LoadedSprites}, ~{stats.MemoryUsageMB:F1} MB in {stats.AtlasCount} atlas");

			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Nyx main thread load failed: {ex.Message}");
			Cleanup();
			return false;
		}
	}

	private void ApplyDemoPlayerStartingItems(Player player)
	{
		static bool TryDemoAdd(ItemContainer container, uint itemTypeId, ushort count, bool mergeWithExisting = true)
		{
			if (ItemsManager.Instance.Get(itemTypeId).IsNone)
			{
				Console.WriteLine($"  Demo items: dat id {itemTypeId} not in loaded client .dat — skipped.");
				return false;
			}

			if (!container.TryAddItemById(itemTypeId, count, mergeWithExisting))
			{
				Console.WriteLine($"  Demo items: could not fit {itemTypeId} ×{count} in backpack.");
				return false;
			}

			return true;
		}

		var eq = player.Equipment;
		eq.Equip(EquipmentSlot.Head, Item.Of(DemoItemIds.Helmet));
		eq.Equip(EquipmentSlot.Body, Item.Of(DemoItemIds.Armor));
		eq.Equip(EquipmentSlot.Legs, Item.Of(DemoItemIds.Legs));
		eq.Equip(EquipmentSlot.LeftHand, Item.Of(DemoItemIds.Katana));
		eq.Equip(EquipmentSlot.RightHand, Item.Of(DemoItemIds.Shield));
		var backpackCap = ItemsManager.Instance.Get(DemoItemIds.Backpack).ContainerCapacity;
		var backpackStorage = new ItemStorage(backpackCap);
		player.SetBackpackStorage(backpackStorage);
		var backpack = new ItemContainer(DemoItemIds.Backpack, 1, backpackStorage);
		eq.Equip(EquipmentSlot.Backpack, backpack);

		var any = TryDemoAdd(backpack, DemoItemIds.GoldCoin, 100);
		any |= TryDemoAdd(backpack, DemoItemIds.Egg, 1, mergeWithExisting: false);
		any |= TryDemoAdd(backpack, DemoItemIds.Egg, 3, mergeWithExisting: false);
		any |= TryDemoAdd(backpack, DemoItemIds.Egg, 6, mergeWithExisting: false);
		any |= TryDemoAdd(backpack, DemoItemIds.Egg, 11, mergeWithExisting: false);
		any |= TryDemoAdd(backpack, DemoItemIds.Egg, 26, mergeWithExisting: false);
		any |= TryDemoAdd(backpack, DemoItemIds.Egg, 80, mergeWithExisting: false);

		if (!any)
		{
			Console.WriteLine("  Demo items: gold/egg missing from client — using fallback stacks in backpack.");
			TryDemoAdd(backpack, DemoItemIds.Katana, 1, mergeWithExisting: false);
			TryDemoAdd(backpack, DemoItemIds.Helmet, 1, mergeWithExisting: false);
			TryDemoAdd(backpack, DemoItemIds.Shield, 1, mergeWithExisting: false);
		}
	}

	public void Update(double deltaTime, IInputContext input, bool blocksMovement = false)
	{
		_protocol.Update(deltaTime);

		if (Player is not null && Map is not null)
		{
			var oldStackX = Player.StackPosition.X;
			var oldStackY = Player.StackPosition.Y;
			var oldStackZ = Player.StackPosition.Z;
			var oldDir = Player.Direction;
			var oldMounted = Player.IsMounted;

			if (!IsNetworkClient)
			{
				_expGainTimer += (float)deltaTime;
				while (_expGainTimer >= 1f)
				{
					_expGainTimer -= 1f;
					var gain = Random.Shared.Next(1, 11);
					Player.Exp += gain;
					ExpGained?.Invoke(gain);
				}
			}

			Player.Update((float)deltaTime, input, Map, blocksMovement);

			if (Player.StackPosition.X != oldStackX || Player.StackPosition.Y != oldStackY || Player.StackPosition.Z != oldStackZ || Player.Direction != oldDir || Player.IsMounted != oldMounted)
			{
				SendLocalPlayerMoveUpdate();
			}
		}

		if (Map is not null)
		{
			foreach (var rp in _remotePlayers)
			{
				rp.Update((float)deltaTime, Map);
			}
		}

		ActiveSpellEffects.Update((float)deltaTime);
		ActiveSpellEffects.Prune();
		ActiveMissileEffects.Update((float)deltaTime);
		ActiveMissileEffects.Prune();
	}

	public void Draw(
		SpriteRenderer renderer,
		NyxRect viewportBounds,
		float shaderTime)
	{
		if (Player is not null && Map is not null && ClientAssets is not null && Drawer is not null)
		{
			var gameW = viewportBounds.Width;
			var gameH = viewportBounds.Height;

			var camXf = 0f;
			var camYf = 0f;
			SpellCastInput.GetCameraOrigin(Player, gameW, gameH, out camXf, out camYf);

			_creatureDrawState.Npcs = Npcs ?? [];
			_creatureDrawState.RemotePlayers = _remotePlayers;
			_creatureDrawState.Player = Player;
			_creatureDrawState.Drawer = Drawer;
			_creatureDrawState.CamXf = camXf;
			_creatureDrawState.CamYf = camYf;

			ClientDraw.DrawMapFloor(
				ClientAssets,
				Drawer,
				Map,
				camXf,
				camYf,
				gameW,
				gameH,
				Player.Position,
				_drawCreaturesAtTile);

			ActiveSpellEffects.Draw(ClientAssets, Drawer, camXf, camYf, gameW, gameH);
			ActiveMissileEffects.Draw(ClientAssets, Drawer, camXf, camYf, gameW, gameH);
		}
	}

	public void InitializeClientNetwork(NetworkManager netManager, string clientId)
	{
		_protocol.InitializeClient(netManager, clientId);
	}

	public bool StartHosting(int port, string serverName)
	{
		return _protocol.StartHosting(port, serverName);
	}

	public void StopHosting()
	{
		_protocol.StopHosting();
	}

	internal void SendLocalPlayerMoveUpdate()
	{
		if (Player is null || !_protocol.IsActive) return;

		var movePacket = new PlayerMovePacket
		{
			ClientId = ClientId,
			PlayerName = Player.Name,
			X = Player.StackPosition.X,
			Y = Player.StackPosition.Y,
			Z = Player.StackPosition.Z,
			Direction = Player.Direction,
			LookType = (ushort)Player.OutfitId,
			LookHead = Player.Appearance.LookHead,
			LookBody = Player.Appearance.LookBody,
			LookLegs = Player.Appearance.LookLegs,
			LookFeet = Player.Appearance.LookFeet,
			LookAddons = Player.Appearance.LookAddons,
			LookMount = (ushort)Player.Appearance.LookMount,
			IsMounted = Player.IsMounted
		};

		byte[] packetData = PacketWriter.WritePacket(movePacket);
		if (IsNetworkClient)
		{
			_protocol.Send(packetData);
		}
		else if (IsNetworkHost)
		{
			_protocol.BroadcastToSpectators(packetData, Player.Position);
		}
	}

	public void SendSpellCastRequest(int slotIndex, float? aimGameX, float? aimGameY, float camXf, float camYf, IInputContext? input)
	{
		if (SpellCatalog is null || !_protocol.IsActive) return;

		var spell = SpellCatalog.Spells[slotIndex];
		int targetX = 0;
		int targetY = 0;
		if (spell.MouseTarget && input is not null)
		{
			SpellCastInput.TryGetMouseTile(input, camXf, camYf, Map!, aimGameX, aimGameY, out targetX, out targetY);
		}
		else if (spell.NeedTarget)
		{
			if (SpellCaster.TryFindTargetTile(Player!, Npcs!, out var tx, out var ty))
			{
				targetX = tx;
				targetY = ty;
			}
		}

		var castPacket = new SpellCastPacket
		{
			SpellId = slotIndex,
			CasterId = ClientId,
			TargetX = targetX,
			TargetY = targetY,
			TargetZ = Player?.Position.Z ?? 7
		};

		byte[] packetData = PacketWriter.WritePacket(castPacket);
		if (IsNetworkClient)
		{
			_protocol.Send(packetData);
		}
		else if (IsNetworkHost)
		{
			_protocol.BroadcastToSpectators(packetData, Player?.Position ?? new Position(0, 0, 7));
			_protocol.HandleSpellCastLocal(castPacket);
		}
	}

	public void SendItemUpdate(int x, int y, int z, ushort itemTypeId, ushort count, bool isPlacement)
	{
		if (!_protocol.IsActive) return;

		var packet = new ItemUpdatePacket
		{
			X = x,
			Y = y,
			Z = z,
			ItemTypeId = itemTypeId,
			Count = count,
			IsPlacement = isPlacement
		};

		byte[] packetData = PacketWriter.WritePacket(packet);
		if (IsNetworkClient)
		{
			_protocol.Send(packetData);
		}
		else if (IsNetworkHost)
		{
			_protocol.BroadcastToSpectators(packetData, new Position(x, y, z));
			_protocol.HandleItemUpdateLocal(packet);
		}
	}

	private void Cleanup()
	{
		_protocol.Dispose();

		ClientAssets?.Dispose();
		ClientAssets = null;
		Drawer = null;
		Player = null;
		Npcs = null;
		Map = null;

		SpellCatalog = null;
	}

	public void Dispose()
	{
		Cleanup();
		GC.SuppressFinalize(this);
	}
}

