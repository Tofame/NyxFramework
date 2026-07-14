# NyxAssets Test Fixtures Catalogue

This directory contains test resources (fixtures) used by the `NyxAssets.Tests` project to verify parsing, loading, and compiling functionality.

All files added inside this directory are automatically configured to be copied to the test execution output folder at build time (via `NyxAssets.Tests.csproj`).

---

## Directory Structure

### 1. Client Assets (`Fixtures/ClientAssets/`)
Store `.dat` and `.spr` files here, organized by protocol version:
*   **`Fixtures/ClientAssets/860/`**
    *   `Tibia.dat` — 860 protocol client structure.
    *   `Tibia.spr` — 860 protocol sprite sheet archive.
*   **`Fixtures/ClientAssets/1098/`**
    *   `Tibia.dat` — 1098 protocol client structure.
    *   `Tibia.spr` — 1098 protocol sprite sheet archive.

### 2. Object Builder Documents (`Fixtures/Obd/`)
Store `.obd` files here:
*   `Fixtures/Obd/item_test.obd` (Legacy / 1098 test object)
*   **`Fixtures/Obd/860/`**
    *   Place 860 protocol OBD files here.
*   **`Fixtures/Obd/1098/`**
    *   Place 1098 protocol OBD files here.

### 3. Compiled Assets (`Fixtures/Assets/`)
Store compiled `.assets` files here (used to test compiled sprite sheet lookup and texture page loading).

### 4. Compiled Things (`Fixtures/Things/`)
Store compiled `.things` files here (used to test compiled item definition loading/caching).

### 5. Extra Sprites (`Fixtures/Sprites/`)
Store raw, individual sprites, custom sheets, or custom formats used to test compilation or compression codecs:
*   Place custom sheets or testing images here.

---

## Accessing Fixtures in Tests

To load fixtures cleanly within unit tests, use a helper method to resolve the absolute path relative to the test assembly's execution path.

```csharp
private static string GetFixturePath(params string[] paths)
{
    var baseDir = AppContext.BaseDirectory;
    return Path.Combine(baseDir, "Fixtures", Path.Combine(paths));
}

// Example usage:
// var datPath = GetFixturePath("ClientAssets", "1098", "Tibia.dat");
// var obdPath = GetFixturePath("Obd", "860", "my_item.obd");
// var assetsPath = GetFixturePath("Assets", "test.assets");
// var thingsPath = GetFixturePath("Things", "test.things");
```
