using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace LightRadiusMod
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        
        /*********
         ** Properties
         *********/
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;
        
        /*********
         ** Public methods
         *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            var harmony = new Harmony(this.ModManifest.UniqueID);
            this.Config = this.Helper.ReadConfig<ModConfig>();
            
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            
            LightPatch.Initialize(Monitor, Config, ModManifest);

            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.initializeLightSource)),
                postfix: new HarmonyMethod(typeof(LightPatch), nameof(LightPatch.initializeLightSource_postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.addLights)),
                postfix: new HarmonyMethod(typeof(LightPatch), nameof(LightPatch.addLights_postfix))
            );
        }

        /*********
         ** Private methods
         *********/

        internal class LightPatch
        {
            private static IMonitor _monitor;
            private static ModConfig _config;
            private static IManifest _manifest;
            private static int num = 1;

            // call this method from your Entry class
            internal static void Initialize(IMonitor monitor, ModConfig config, IManifest manifest)
            {
                _monitor = monitor;
                _config = config;
                _manifest = manifest;
            }

            internal static void addLights_postfix(ref Furniture __instance)
            {
                GameLocation environment = __instance.Location;
                
                if (environment == null)
                {
                    return;
                }
                if (__instance.furniture_type.Value == 7 || __instance.furniture_type.Value == 17 ||
                    __instance.QualifiedItemId == "(F)1369")
                {
                    if (!__instance.modData.ContainsKey("{this.ModManifest.UniqueID}/base-radius"))
                        __instance.modData.Add("{this.ModManifest.UniqueID}/base-radius", __instance.lightSource.radius.Value.ToString());
                    int textureIndex = __instance.lightSource.textureIndex.Value;
                    Vector2 position = __instance.lightSource.position.Value;
                    int.TryParse(__instance.modData["{this.ModManifest.UniqueID}/base-radius"], out int baseRadius);
                    float radius = baseRadius * _config.FurnitureLightRadius;
                    Color color = __instance.lightSource.color.Value;
                    string id = __instance.lightSource.Id;
                    LightSource.LightContext lightContext = __instance.lightSource.lightContext.Value;
                    long playerId = __instance.lightSource.PlayerID;
                
                    __instance.lightSource = new LightSource(id, textureIndex, position, radius, color, lightContext, playerId);
                    environment.sharedLights.AddLight(__instance.lightSource.Clone());
                }
            }
            
            internal static void initializeLightSource_postfix(ref Object __instance)
            {
                if (__instance.lightSource != null)
                {
                    if (!__instance.modData.ContainsKey("{this.ModManifest.UniqueID}/base-radius"))
                        __instance.modData.Add("{this.ModManifest.UniqueID}/base-radius", __instance.lightSource.radius.Value.ToString());
                    int textureIndex = __instance.lightSource.textureIndex.Value;
                    Vector2 position = __instance.lightSource.position.Value;
                    int.TryParse(__instance.modData["{this.ModManifest.UniqueID}/base-radius"], out int baseRadius);
                    float radius = baseRadius * _config.ObjectLightRadius;
                    Color color = __instance.lightSource.color.Value;
                    string id = __instance.lightSource.Id;
                    LightSource.LightContext lightContext = __instance.lightSource.lightContext.Value;
                    long playerId = __instance.lightSource.PlayerID;
                
                    __instance.lightSource = new LightSource(id, textureIndex, position, radius, color, lightContext, playerId);
                }
            }
        }
        
        
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;
            
            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)//,
                // titleScreenOnly: true
            );
            
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Furniture Light Radius Multiplier",
                tooltip: () => "The amount to make all furniture (indoor) lights bigger by.",
                getValue: () => this.Config.FurnitureLightRadius,
                setValue: value => this.Config.FurnitureLightRadius = value
            );
            
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Object Light Radius Multiplier",
                tooltip: () => "The amount to make all non-furniture lights bigger by.",
                getValue: () => this.Config.ObjectLightRadius,
                setValue: value => this.Config.ObjectLightRadius = value
            );
        }
    }
}