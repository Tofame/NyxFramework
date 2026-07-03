Shader secondary textures (optional).

Place PNG files here:

  rainbow.png   — map_rainbow (procedural strip if missing)
  gold.png      — outfit_gold
  snow.png      — map_snow

Lookup order at runtime (next to the executable):
  1) <app>/resources/images/rainbow.png
  2) <app>/resources/images/shaders/rainbow.png   <-- this folder (preferred)

Shaders themselves live in Sandbox/shaders/*.frag and are copied to <app>/shaders/.

Override shader root with env NYXFRAMEWORK_SHADERS or NYXFRAMEWORK_WOS_DATA.
