using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Characters;
using StardewValley;

namespace ChickenStatueDoesSomething
{
    public class ModEntry : Mod
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
            this.Config = this.Helper.ReadConfig<ModConfig>();

            Helper.Events.GameLoop.GameLaunched += (e, a) => OnGameLaunched(e, a);
            Helper.Events.GameLoop.DayStarted += (e, a) => OnDayStarted(e, a);
        }

        /// <summary>Add to Generic Mod Config Menu</summary>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add config options
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Options_FriendshipBonus"),
                getValue: () => this.Config.FriendshipBonus,
                setValue: value => this.Config.FriendshipBonus = (int)value,
                min: 0,
                max: 1000
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Options_MoodBonus"),
                getValue: () => this.Config.MoodBonus,
                setValue: value => this.Config.FriendshipBonus = (int)value,
                min: 0,
                max: 255
            );
        }

        /// <summary>Check for bonuses at start of day</summary>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Utility.ForEachLocation((Func<GameLocation, bool>)(location =>
            {
                foreach (var farmAnimal in location.animals.Values)
                {
                    checkForBonus(farmAnimal);
                }
                return true;
            }));
        }

        private void checkForBonus(FarmAnimal farmAnimal)
        {
            this.Monitor.Log($"[Chicken Statue Does Something] Checking {farmAnimal.Name} ({farmAnimal.type.Value})", LogLevel.Trace);

            // Is it a chicken?
            if (!farmAnimal.type.Value.Contains("Chicken"))
            {
                return;
            }

            // Does their coop contain a Chicken Statue?
            var hasChickenStatue = false;
            Utility.ForEachItemIn(farmAnimal.homeInterior, (Func<Item, bool>)(item => {
                this.Monitor.Log($"[Chicken Statue Does Something] Checking item ID {item.QualifiedItemId} = {item.Name}", LogLevel.Trace);
                var currentObject = (StardewValley.Object)item;
                if (currentObject.QualifiedItemId == "(F)1305" || currentObject.QualifiedItemId == "(O)113")
                {
                    hasChickenStatue = true;
                    return false; // skip remaining items
                }
                return true;
            }));
            if (!hasChickenStatue)
            {
                return;
            }

            // Apply friendship bonus
            var oldFriendship = farmAnimal.friendshipTowardFarmer.Value;
            var newFriendship = Math.Min(1000, farmAnimal.friendshipTowardFarmer.Value + this.Config.FriendshipBonus);
            if (newFriendship > oldFriendship)
            {
                farmAnimal.friendshipTowardFarmer.Value = newFriendship;
                this.Monitor.Log($"[Chicken Statue Does Something] {farmAnimal.Name} friendship raised from {oldFriendship} to {newFriendship}", LogLevel.Debug);
            }
            else
            {
                this.Monitor.Log($"[Chicken Statue Does Something] {farmAnimal.Name} already at max friendship", LogLevel.Debug);
            }

            // Apply mood bonus
            var oldMood = farmAnimal.happiness.Value;
            var newMood = (int)(byte)Math.Min((int)byte.MaxValue, farmAnimal.happiness.Value + this.Config.MoodBonus);
            if (newMood > oldMood)
            {
                farmAnimal.happiness.Value = newMood;
                this.Monitor.Log($"[Chicken Statue Does Something] {farmAnimal.Name} mood raised from {oldMood} to {newMood}", LogLevel.Debug);
            }
            else
            {
                this.Monitor.Log($"[Chicken Statue Does Something] {farmAnimal.Name} already at max mood", LogLevel.Debug);
            }
        }
    }
}