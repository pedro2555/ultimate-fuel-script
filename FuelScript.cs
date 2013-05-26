using GTA;
using SlimDX.XInput;
using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace UltimateFuelScript
{
    public class FuelScript : Script
    {
        /// <summary>
        /// Start the script
        /// 
        /// RELEASE WARNING, SlimDX.dll should be placed on GTA root folder, NOT the scripts folder.
        /// 
        /// </summary>
        public FuelScript()
        {
            SettingsFile.Open("FuelScript.ini");
            Settings.Load();

            Log("UltimateFuelScriptV3", "Started under GTA " + Game.Version.ToString());
            Log("UltimateFuelScriptV3", "dsound.dll " + ((File.Exists(Game.InstallFolder + "\\dsound.dll")) ? "present" : "not present"));
            Log("UltimateFuelScriptV3", "xlive.dll " + ((File.Exists(Game.InstallFolder + "\\xlive.dll")) ? "present" : "not present"));
            Log("UltimateFuelScriptV3", "SlimDX.dll " + ((File.Exists(Game.InstallFolder + "\\SlimDX.dll")) ? "present" : "not present"));
            Log("UltimateFuelScriptV3", "OS Version: " + getOSInfo());

            reFuel = false;
            dashBoardLocation = new PointF(Settings.GetValueFloat("X", "DASHBOARD", 0.0f), Settings.GetValueFloat("Y", "DASHBOARD", 0.0f));
            SpeedMultiplier = (Settings.GetValueString("SPEED", "MISC", "KPH").ToUpper().Trim() == "KPH") ? 3.6f : 2.23693629f;
            GaugeWidth = Settings.GetValueFloat("CLASSICGAUGEWITH", "DASHBOARD", 0.0f);

            if (Settings.GetValueBool("GAMEPAD", "MISC", false))
                GamePad = new Controller(UserIndex.One);
            else
                GamePad = null;
            this.isOnReserve = false;

            /// <summary>
            /// Loads all the stations.
            /// There are 3 types of stations:
            ///     STATION,
            ///     HELISTATION,
            ///     BOATSTATION.
            ///     
            ///  All types are self explanatory. Every station of a specified type as an unique identifier,
            ///  preceded by the station type keyword.
            /// 
            /// Each type can have up to 254 stations.
            /// 
            /// The first identifier is 1, the last is 255.
            /// 
            /// The identifiers must be consecutive (1, 2, 3, 4. NOT 1, 3, 5)
            /// </summary>
            #region load fuel stations
            try
            {
                // load car fuel stations
                if (Settings.GetValueBool("CARS", "MISC", true))
                {
                    for (byte i = 1; i <= Byte.MaxValue; i++)
                    {
                        Vector3 loc = Settings.GetValueVector3("LOCATION", "STATION" + i, new Vector3(-123456789.0987654321f, -123456789.0987654321f, -123456789.0987654321f));
                        if (loc.X == -123456789.0987654321f && loc.Y == -123456789.0987654321f && loc.Z == -123456789.0987654321f)
                            break;
                        else
                        {
                            Blip b = GTA.Blip.AddBlip(loc);
                            b.Icon = (BlipIcon)79;
                            b.Name = (Settings.GetValueString("NAME", "STATION" + i, "Fuel Station").ToUpper().Trim().Length > 30) ? Settings.GetValueString("NAME", "STATION" + i, "Fuel Station").ToUpper().Trim().Substring(0, 29) : Settings.GetValueString("NAME", "STATION" + i, "Fuel Station").ToUpper().Trim();
                            b.Display = BlipDisplay.MapOnly;
                            b.Friendly = true;
                            b.RouteActive = false;
                            b.ShowOnlyWhenNear = true;
                        }
                    }
                }
                if (Settings.GetValueBool("HELIS", "MISC", true))
                {
                    // load heli fuel stations
                    for (byte i = 1; i <= Byte.MaxValue; i++)
                    {
                        Vector3 loc = Settings.GetValueVector3("LOCATION", "HELISTATION" + i, new Vector3(-123456789.0987654321f, -123456789.0987654321f, -123456789.0987654321f));
                        if (loc.X == -123456789.0987654321f && loc.Y == -123456789.0987654321f && loc.Z == -123456789.0987654321f)
                            break;
                        else
                        {
                            Blip b = GTA.Blip.AddBlip(loc);
                            b.Icon = (BlipIcon)56;
                            b.Name = (Settings.GetValueString("NAME", "HELISTATION" + i, "Fuel Station").ToUpper().Trim().Length > 30) ? Settings.GetValueString("NAME", "HELISTATION" + i, "Fuel Station").ToUpper().Trim().Substring(0, 29) : Settings.GetValueString("NAME", "HELISTATION" + i, "Fuel Station").ToUpper().Trim();
                            b.Display = BlipDisplay.MapOnly;
                            b.Friendly = true;
                            b.RouteActive = false;
                            b.ShowOnlyWhenNear = true;
                        }
                    }
                }
                if (Settings.GetValueBool("BOATS", "MISC", true))
                {
                    // load boat fuel stations
                    for (byte i = 1; i <= Byte.MaxValue; i++)
                    {
                        Vector3 loc = Settings.GetValueVector3("LOCATION", "BOATSTATION" + i, new Vector3(-123456789.0987654321f, -123456789.0987654321f, -123456789.0987654321f));
                        if (loc.X == -123456789.0987654321f && loc.Y == -123456789.0987654321f && loc.Z == -123456789.0987654321f)
                            break;
                        else
                        {
                            Blip b = GTA.Blip.AddBlip(loc);
                            b.Icon = (BlipIcon)48;
                            b.Name = (Settings.GetValueString("NAME", "BOATSTATION" + i, "Fuel Station").ToUpper().Trim().Length > 30) ? Settings.GetValueString("NAME", "BOATSTATION" + i, "Fuel Station").ToUpper().Trim().Substring(0, 29) : Settings.GetValueString("NAME", "BOATSTATION" + i, "Fuel Station").ToUpper().Trim();
                            b.Display = BlipDisplay.MapOnly;
                            b.Friendly = true;
                            b.RouteActive = false;
                            b.ShowOnlyWhenNear = true;
                        }
                    }
                }
            }
            catch (Exception crap) { Log("Loading stations", crap.Message); }
            #endregion


            this.Interval = 1000;
            this.Tick += new EventHandler(carScript_Tick);

            switch (Settings.GetValueString("MODE", "DASHBOARD", "CLASSIC").ToUpper().Trim())
            {
                case "DEV":
                    this.PerFrameDrawing += new GraphicsEventHandler(carScript_PerFrameDrawing_devMode);
                    this.BindKey(Keys.Q, Settings.Load);
                    break;

                case "DIGITAL":
                    this.PerFrameDrawing += new GraphicsEventHandler(carScript_PerFrameDrawing_digitalMode);
                    break;

                case "CLASSIC":
                    this.PerFrameDrawing += new GraphicsEventHandler(carScript_PerFrameDrawing_classicMode);
                    break;
            }

            this.KeyDown += new GTA.KeyEventHandler(UltimateFuelScriptV2_KeyDown);
            this.KeyUp += new GTA.KeyEventHandler(UltimateFuelScriptV2_KeyUp);
        }

        #region variables and properties
        /// <summary>
        /// Determines if the currVeh as already entered the reserve level.
        /// </summary>
        private bool isOnReserve;
        /// <summary>
        /// Used for classic mode only.
        /// </summary>
        private float GaugeWidth;
        /// <summary>
        /// To keep track of the flashing sequence in reserve levels, this can probably be changed to a lower allocation later
        /// </summary>
        private int flash = 0;
        /// <summary>
        /// mps to knots
        /// </summary>
        private const float Knots = 1.94384449f;
        /// <summary>
        /// Determines if speed is shown in KPH or MPH
        /// </summary>
        private float SpeedMultiplier;
        /// <summary>
        /// The location of the dash board
        /// </summary>
        private PointF dashBoardLocation;
        /// <summary>
        /// Holds the last vehicle the player has driven
        /// </summary>
        private Vehicle LastVehicle;
        /// <summary>
        /// Returns true if the player is refueling.
        /// </summary>
        private bool reFuel;
        /// <summary>
        /// Used to debt to the total money from the player's money value.
        /// </summary>
        private float reFuelAmount;
        /// <summary>
        /// Only used in devMode
        /// </summary>
        private float drainPerSecond;
        /// <summary>
        /// Current game, if aplicable.
        /// </summary>
        private Controller GamePad;
        /// <summary>
        /// Alias for Player.Character.CurrentVehicle
        /// </summary>
        private Vehicle currVeh
        { get { return Player.Character.CurrentVehicle; } }
        #endregion

        #region methods
        /// <summary>
        /// Get os name and SP
        /// </summary>
        /// <returns></returns>
        string getOSInfo()
        {
            //Get Operating system information.
            OperatingSystem os = Environment.OSVersion;
            //Get version information about the os.
            Version vs = os.Version;

            //Variable to hold our return value
            string operatingSystem = "";

            if (os.Platform == PlatformID.Win32Windows)
            {
                //This is a pre-NT version of Windows
                switch (vs.Minor)
                {
                    case 0:
                        operatingSystem = "95";
                        break;
                    case 10:
                        if (vs.Revision.ToString() == "2222A")
                            operatingSystem = "98SE";
                        else
                            operatingSystem = "98";
                        break;
                    case 90:
                        operatingSystem = "Me";
                        break;
                    default:
                        break;
                }
            }
            else if (os.Platform == PlatformID.Win32NT)
            {
                switch (vs.Major)
                {
                    case 3:
                        operatingSystem = "NT 3.51";
                        break;
                    case 4:
                        operatingSystem = "NT 4.0";
                        break;
                    case 5:
                        if (vs.Minor == 0)
                            operatingSystem = "2000";
                        else
                            operatingSystem = "XP";
                        break;
                    case 6:
                        if (vs.Minor == 0)
                            operatingSystem = "Vista";
                        else
                            operatingSystem = "7";
                        break;
                    default:
                        break;
                }
            }
            //Make sure we actually got something in our OS check
            //We don't want to just return " Service Pack 2" or " 32-bit"
            //That information is useless without the OS version.
            if (operatingSystem != "")
            {
                //Got something.  Let's prepend "Windows" and get more info.
                operatingSystem = "Windows " + operatingSystem;
                //See if there's a service pack installed.
                if (os.ServicePack != "")
                {
                    //Append it to the OS name.  i.e. "Windows XP Service Pack 3"
                    operatingSystem += " " + os.ServicePack;
                }
                //Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"
                operatingSystem += " " + getOSArchitecture().ToString() + "-bit";
            }
            //Return the information we've gathered.
            return operatingSystem;
        }
        /// <summary>
        /// Get OS architecture in use.
        /// </summary>
        /// <returns></returns>
        int getOSArchitecture()
        {
            string pa = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            return ((String.IsNullOrEmpty(pa) || String.Compare(pa, 0, "x86", 0, 3, true) == 0) ? 32 : 64);
        }
        /// <summary>
        /// Saves an exception's message with the current date and time, and the method that originated it.
        /// </summary>
        /// <param name="methodName">The method that originated it</param>
        /// <param name="message">The exception's message</param>
        private void Log(string methodName, string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                    using (StreamWriter streamWriter = File.AppendText(Application.StartupPath + "\\scripts\\FuelScript.log"))
                    {
                        streamWriter.WriteLine(System.DateTime.Now + " @ " + methodName + " - " + message);
                        streamWriter.Close();
                    }
            }
            catch { }
            finally
            {
#if DEBUG
                Game.DisplayText("Check log - " + message, 3000);
#endif
            }

        }
        /// <summary>
        /// Use ONLY when player is in vehicle!
        /// 
        /// Retrieves the values of DRAIN, MAXTANK and RESERVE, from the loaded ini file. First looks for the car hash, then for the car name, then defaults.
        /// 
        /// Generates a random amount of fuel, if an amount can't be find under currVeh.Metadata.Fuel.
        /// 
        /// Calculates the amount of fuel to be drain and drains it, and prevents the car engine from running when out of fuel.
        /// 
        /// </summary>
        private void DrainFuel()
        {
            try
            {
                #region check if Fuel values exists
                try { float f = currVeh.Metadata.Fuel; }
                catch
                {
                    // if it does not exists
                    // first the ini file is checked for the vehicle's hash code
                    // then for the vehicle's name
                    if (currVeh.Model.isCar || currVeh.Model.isBike)
                    {
                        currVeh.Metadata.MaxTank = Settings.GetValueInteger("TANK", currVeh.GetHashCode().ToString(),
                        Settings.GetValueInteger("TANK", currVeh.Name, 100));
                        currVeh.Metadata.Drain = Settings.GetValueInteger("DRAIN", currVeh.GetHashCode().ToString(),
                        Settings.GetValueInteger("DRAIN", currVeh.Name, 10));
                        currVeh.Metadata.Reserve = Settings.GetValueInteger("RESERVE", currVeh.GetHashCode().ToString(),
                        Settings.GetValueInteger("RESERVE", currVeh.Name, 10));
                        currVeh.Metadata.Fuel = (int)new Random().Next(currVeh.Metadata.Reserve + 1, currVeh.Metadata.MaxTank);
                    }
                    else if (currVeh.Model.isHelicopter || currVeh.Model.isBoat)
                    {
                        currVeh.Metadata.MaxTank = Settings.GetValueInteger("TANK", currVeh.GetHashCode().ToString(),
                        Settings.GetValueInteger("TANK", currVeh.Name, 100));
                        currVeh.Metadata.Drain = Settings.GetValueInteger("DRAIN", currVeh.GetHashCode().ToString(),
                        Settings.GetValueInteger("DRAIN", currVeh.Name, 10));
                        currVeh.Metadata.Reserve = Settings.GetValueInteger("RESERVE", currVeh.GetHashCode().ToString(),
                        Settings.GetValueInteger("RESERVE", currVeh.Name, 10));
                        currVeh.Metadata.Fuel = (int)new Random().Next(currVeh.Metadata.Reserve + 1, currVeh.Metadata.MaxTank);
                    }
                }
                #endregion

                #region take in account fuel drain values from ini file and engine health

                if (currVeh.Metadata.Fuel > 0.0f)
                {
                    currVeh.HazardLightsOn = false;
                    if ((currVeh.Model.isCar || currVeh.Model.isBike) && currVeh.EngineRunning && Settings.GetValueBool("CARS", "MISC", true))
                    {
                        // Code for cars and bikes

                        //currVeh.Metadata.Drain is a user defined constant, defaults to 20
                        drainPerSecond = currVeh.Metadata.Drain * currVeh.CurrentRPM / 100;
                        // increase consumption based on engine damage 
                        drainPerSecond = drainPerSecond * ((1000 - currVeh.EngineHealth) / 1000) + drainPerSecond;
                        // actually remove the calculated value
                        currVeh.Metadata.Fuel -= drainPerSecond;
                        // avoid negative values
                        currVeh.Metadata.Fuel = (currVeh.Metadata.Fuel < 0.0f) ? 0.0f : currVeh.Metadata.Fuel;
                    }
                    else if (currVeh.Model.isHelicopter && currVeh.EngineRunning && Settings.GetValueBool("HELIS", "MISC", true))
                    {
                        // Code for Helis

                        // 254.921568627451f

                        // 0.2 + ((speed * 0.2) / 5)
                        // only take in account speed when : accelerate xor reverse key is pressed

                        if (GamePad == null)
                            if (Game.isGameKeyPressed(GameKey.MoveForward))
                                drainPerSecond = (currVeh.Metadata.Drain * (.2f + ((currVeh.Speed * .2f) / 5.0f))) / 100.0f;
                            else
                                drainPerSecond = (currVeh.Metadata.Drain * .208f) / 100.0f;
                        else if (GamePad.GetState().Gamepad.RightTrigger > 0.0f)
                            drainPerSecond = currVeh.Metadata.Drain * (((GamePad.GetState().Gamepad.RightTrigger * 100.0f) / 255.0f) / 10000.0f);
                        else
                            drainPerSecond = (currVeh.Metadata.Drain * .208f) / 100.0f;

                        drainPerSecond = drainPerSecond * ((1000 - currVeh.EngineHealth) / 1000.0f) + drainPerSecond;
                        currVeh.Metadata.Fuel -= drainPerSecond;
                        currVeh.Metadata.Fuel = (currVeh.Metadata.Fuel < .0f) ? .0f : currVeh.Metadata.Fuel;
                    }
                    else if (currVeh.Model.isBoat && currVeh.EngineRunning && Settings.GetValueBool("BOATS", "MISC", true))
                    {
                        // Code for boats

                        // 0.2 + ((speed * 0.2) / 5)
                        // only take in account speed when accelerate xor reverse key is pressed
                        if (GamePad == null)
                            if (Game.isGameKeyPressed(GameKey.MoveForward) ^ Game.isGameKeyPressed(GameKey.MoveBackward))
                                drainPerSecond = (currVeh.Metadata.Drain * (.2f + ((currVeh.Speed * .2f) / 5.0f))) / 100;
                            else
                                drainPerSecond = (currVeh.Metadata.Drain * .208f) / 100;
                        else
                            if (GamePad.GetState().Gamepad.RightTrigger > 0 ^ GamePad.GetState().Gamepad.LeftTrigger > 0)
                                drainPerSecond = (currVeh.Metadata.Drain * (.2f + ((currVeh.Speed * .2f) / 5.0f))) / 100;
                            else
                                drainPerSecond = (currVeh.Metadata.Drain * .208f) / 100;

                        drainPerSecond = drainPerSecond * ((1000 - currVeh.EngineHealth) / 1000) + drainPerSecond;
                        currVeh.Metadata.Fuel -= drainPerSecond;
                        currVeh.Metadata.Fuel = (currVeh.Metadata.Fuel < .0f) ? .0f : currVeh.Metadata.Fuel;
                    }

                    if (!isOnReserve && currVeh.Metadata.Fuel <= currVeh.Metadata.Reserve && currVeh.EngineRunning && currVeh.Speed > 2.5f)
                    {
                        isOnReserve = true;
                        Play("resSound");
                    }
                    else if (currVeh.Metadata.Fuel > currVeh.Metadata.Reserve)
                        isOnReserve = false;
                }
                else
                    if (GTA.Native.Function.Call<bool>("IS_MOBILE_PHONE_CALL_ONGOING") && currVeh.Metadata.Fuel == 0)
                    {
                        currVeh.Metadata.Fuel = currVeh.Metadata.Reserve + 0.5f;
                        isOnReserve = false;
                        currVeh.EngineRunning = true;
                        currVeh.HazardLightsOn = false;
                    }
                    else
                    {
                        currVeh.EngineRunning = false;
                        currVeh.HazardLightsOn = true;
                        currVeh.Metadata.Fuel = 0;
                    }

                #endregion
            }
            catch (Exception crap) { Log("DrainFuel", crap.Message); }
        }
        /// <summary>
        /// Use if not sure if player is in vehicle.
        /// </summary>
        /// <returns></returns>
        private bool isAtFuelStationGeneric()
        {
            try
            {
                for (int i = 1; i < 1000; i++)
                {
                    Vector3 loc = Settings.GetValueVector3("LOCATION", "STATION" + i, new Vector3(-123456789.0987654321f, -123456789.0987654321f, -123456789.0987654321f));
                    if (loc.X == -123456789.0987654321f && loc.Y == -123456789.0987654321f && loc.Z == -123456789.0987654321f)
                        break;
                    else if (loc.DistanceTo(Player.Character.Position) < Settings.GetValueFloat("RADIUS", "STATION" + i, 10))
                        return true;
                }
                for (int i = 1; i < 1000; i++)
                {
                    Vector3 loc = Settings.GetValueVector3("LOCATION", "BOATSTATION" + i, new Vector3(-123456789.0987654321f, -123456789.0987654321f, -123456789.0987654321f));
                    if (loc.X == -123456789.0987654321f && loc.Y == -123456789.0987654321f && loc.Z == -123456789.0987654321f)
                        break;
                    else if (loc.DistanceTo(Player.Character.Position) < Settings.GetValueFloat("RADIUS", "BOATSTATION" + i, 10))
                        return true;
                }
                for (int i = 1; i < 1000; i++)
                {
                    Vector3 loc = Settings.GetValueVector3("LOCATION", "HELISTATION" + i, new Vector3(-123456789.0987654321f, -123456789.0987654321f, -123456789.0987654321f));
                    if (loc.X == -123456789.0987654321f && loc.Y == -123456789.0987654321f && loc.Z == -123456789.0987654321f)
                        break;
                    else if (loc.DistanceTo(Player.Character.Position) < Settings.GetValueFloat("RADIUS", "HELISTATION" + i, 10))
                        return true;
                }
                return false;
            }
            catch (Exception crap) { Log("isAtFuelStationGeneric", crap.Message); return false; }
        }
        /// <summary>
        /// Use ONLY when player is in vehicle!
        /// 
        /// Returns the station id, if the player is at any station valid for the vehicle type.
        /// </summary>
        /// <returns></returns>
        private int isAtFuelStation()
        {
            try
            {
                string toLookFor = (currVeh.Model.isHelicopter) ? "HELISTATION" : (currVeh.Model.isBoat) ? "BOATSTATION" : "STATION";
                for (int i = 1; i < 1000; i++)
                {
                    Vector3 loc = Settings.GetValueVector3("LOCATION", toLookFor + i, new Vector3(-123456789.0987654321f, -123456789.0987654321f, -123456789.0987654321f));
                    if (loc.X == -123456789.0987654321f && loc.Y == -123456789.0987654321f && loc.Z == -123456789.0987654321f)
                        break;
                    else if (loc.DistanceTo(Player.Character.Position) < Settings.GetValueFloat("RADIUS", "STATION" + i, 10))
                        return i;
                }
                return -1;
            }
            catch (Exception crap) { Log("isAtFuelStation", crap.Message); return -1; }
        }
        /// <summary>
        /// Finishes the reFuel process.
        /// 
        /// Debts the money from the player's money value, and allows the car to be started.
        /// </summary>
        private void FinishRefuel()
        {
            try
            {
                if (reFuel)
                {
                    string station = (currVeh.Model.isBoat) ? "BOATSTATION" : (currVeh.Model.isHelicopter) ? "HELISTATION" : "STATION";
                    if (Settings.GetValueInteger("STARS", station + isAtFuelStation(), 0) == 0)
                        Player.Money -= Convert.ToInt32((reFuelAmount * Settings.GetValueFloat("PRICE", station + isAtFuelStation(), 2.99f)));
                    reFuelAmount = 0.0f;
                    Player.WantedLevel = (Settings.GetValueInteger("STARS", station + isAtFuelStation(), 0) > 0 && Player.WantedLevel < Settings.GetValueInteger("STARS", station + isAtFuelStation(), 0)) ? Settings.GetValueInteger("STARS", station + isAtFuelStation(), 0) : Player.WantedLevel;
                    reFuel = false;
                    currVeh.EngineRunning = true;
                    currVeh.NeedsToBeHotwired = false;
                    GTA.Native.Function.Call("SET_VEH_LIGHTS", currVeh, 2);
                    GTA.Native.Function.Call("DISPLAY_CASH", true);
                }
            }
            catch (Exception crap) { Log("FinishRefuel", crap.Message); }
        }
        /// <summary>
        /// Fills the fuel tank at 3 untis per second in case of cars and bikes, fills at 19 units per second in case of boats and helis.
        /// </summary>
        private void ReFuel()
        {
            try
            {
                // fills at 3 units per second
                float unitsPerSecond = (currVeh.Model.isCar || currVeh.Model.isBike) ? 3 : 19;
                if (currVeh.Metadata.Fuel >= currVeh.Metadata.MaxTank)
                {
                    // tank is full
                    currVeh.Metadata.Fuel = currVeh.Metadata.MaxTank;
                    FinishRefuel();
                }
                else
                {
                    float amount = (currVeh.Metadata.Fuel + unitsPerSecond > currVeh.Metadata.MaxTank) ? currVeh.Metadata.MaxTank - currVeh.Metadata.Fuel : unitsPerSecond;
                    if (Player.Money < (amount * Settings.GetValueFloat("PRICE", "STATION" + isAtFuelStation(), 2.99f)) + (reFuelAmount * Settings.GetValueFloat("PRICE", (currVeh.Model.isBoat) ? "BOATSTATION" : (currVeh.Model.isHelicopter) ? "HELISTATION" : "STATION" + isAtFuelStation(), 2.99f)))
                        // Player does not have any money
                        FinishRefuel();
                    else
                    {
                        // refuel
                        currVeh.Metadata.Fuel += amount;
                        reFuelAmount += amount;
                    }
                }
            }
            catch (Exception crap) { Log("ReFuel", crap.Message); }
        }
        /// <summary>
        /// Play a specific sound from the embedded resources
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Play(string sound)
        {
            try
            {
                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                System.IO.Stream s = a.GetManifestResourceStream("UltimateFuelScript." + sound + ".wav");
                SoundPlayer player = new SoundPlayer(s);
                player.Play();
            }
            catch (Exception crap) { Log("Play", crap.Message); }
        }

        #endregion

        #region key bindings
        /// <summary>
        /// Handles the REFUELKEY's up event behaviour.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UltimateFuelScriptV2_KeyUp(object sender, GTA.KeyEventArgs e)
        {
            FinishRefuel();
        }
        /// <summary>
        /// Handles the REFUELKEY's down event behaviour.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UltimateFuelScriptV2_KeyDown(object sender, GTA.KeyEventArgs e)
        {
            if (!reFuel && Player.Character.isInVehicle() && e.Key == Settings.GetValueKey("REFUELKEY", "KEYS", Keys.X) && isAtFuelStation() > -1)
            {
                // debt total from player's money

                reFuelAmount = 0.0f;
                reFuel = true;
            }
        }
        #endregion

        #region events
        /// <summary>
        /// run every second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void carScript_Tick(object sender, EventArgs e)
        {
            try
            {
                if (Player.Character.isInVehicle())
                    if (Player == currVeh.GetPedOnSeat(VehicleSeat.Driver))
                    {


                        if (reFuel)
                            // Refuel
                            ReFuel();
                        else
                        {
                            // Locks the doors if above the doorlockspeed speed value
                            // Take care of fuel draining
                            DrainFuel();
                        }

                        if (LastVehicle == null || currVeh != LastVehicle)
                            isOnReserve = false;
                        LastVehicle = currVeh;
                    }
                    else
                        LastVehicle = null;
                else
                    LastVehicle = null;
            }
            catch (Exception crap)
            {
                Log("carScript_Tick", crap.Message);
            }
        }
        /// <summary>
        /// run every frame, devMode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void carScript_PerFrameDrawing_devMode(object sender, GTA.GraphicsEventArgs e)
        {
            try
            {
                if (reFuel)
                {
                    Player.Character.Task.ClearSecondary();
                    GTA.Native.Function.Call("FORCE_CAR_LIGHTS", currVeh, 0);
                    currVeh.EngineRunning = false;
                }

                e.Graphics.Scaling = FontScaling.ScreenUnits;
                PointF dashBoardLocation = new PointF(Settings.GetValueFloat("X", "DASHBOARD", 0.0f), Settings.GetValueFloat("Y", "DASHBOARD", 0.0f));

                if (Player.Character.isInVehicle())
                {
                    try { e.Graphics.DrawText("FUEL".PadRight(15) + currVeh.Metadata.Fuel, dashBoardLocation.X, dashBoardLocation.Y + 0.02f); }
                    catch { }
                    e.Graphics.DrawText("SPEED".PadRight(15) + "\t" + currVeh.Speed * 3.6f, dashBoardLocation.X, dashBoardLocation.Y + 0.04f);
                    e.Graphics.DrawText("ENGINE".PadRight(15) + "\t" + currVeh.EngineHealth, dashBoardLocation.X, dashBoardLocation.Y + 0.06f);
                    e.Graphics.DrawText("RPM".PadRight(15) + "\t" + currVeh.CurrentRPM, dashBoardLocation.X, dashBoardLocation.Y + 0.08f);
                    e.Graphics.DrawText("HASH".PadRight(15) + "\t" + currVeh.Model.Hash, dashBoardLocation.X, dashBoardLocation.Y + 0.1f);
                    e.Graphics.DrawText("NAME".PadRight(15) + "\t" + currVeh.Name, dashBoardLocation.X, dashBoardLocation.Y + 0.12f);
                    e.Graphics.DrawText("DRAIN/Sec".PadRight(15) + "\t" + drainPerSecond, dashBoardLocation.X, dashBoardLocation.Y + 0.14f);
                    e.Graphics.DrawText("DOOR".PadRight(15) + "\t" + ((currVeh.DoorLock == DoorLock.None) ? "UNLOCKED" : ((currVeh.DoorLock == DoorLock.CanOpenFromInside) ? "OUT LOCK" : "FULL LOCK")), dashBoardLocation.X, dashBoardLocation.Y + 0.16f);
                }

                e.Graphics.DrawText("LOCATION", dashBoardLocation.X, dashBoardLocation.Y + 0.2f);
                e.Graphics.DrawText(Player.Character.Position.X + ", " + Player.Character.Position.Y + ", " + Player.Character.Position.Z, dashBoardLocation.X, dashBoardLocation.Y + 0.22f);

            }
            catch (Exception crap)
            {
                Log("carScript_PerFrameDrawing", crap.Message);
            }
        }
        /// <summary>
        /// run every frame, digitalMode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void carScript_PerFrameDrawing_digitalMode(object sender, GTA.GraphicsEventArgs e)
        {
            try
            {
                if (reFuel)
                {
                    GTA.Native.Function.Call("FORCE_CAR_LIGHTS", currVeh, 0);
                    currVeh.EngineRunning = false;
                }

                e.Graphics.Scaling = FontScaling.ScreenUnits;

                if (Player.Character.isInVehicle())
                {
                    try
                    {
                        e.Graphics.DrawText("FUEL", dashBoardLocation.X, dashBoardLocation.Y, Color.Beige);
                        e.Graphics.DrawText(Convert.ToInt32((float)currVeh.Metadata.Fuel).ToString(), dashBoardLocation.X + 0.06f, dashBoardLocation.Y, (currVeh.Metadata.Fuel <= currVeh.Metadata.Reserve) ? Color.Red : Color.Green);
                    }
                    catch
                    {
                    }

                    e.Graphics.DrawText("SPEED", dashBoardLocation.X, dashBoardLocation.Y + 0.03f);
                    if (currVeh.Model.isBoat)
                    {
                        e.Graphics.DrawText(Convert.ToInt32(currVeh.Speed * Knots).ToString(), dashBoardLocation.X + 0.06f, dashBoardLocation.Y + 0.03f);
                        e.Graphics.DrawText("Knots", dashBoardLocation.X + 0.09f, dashBoardLocation.Y + 0.03f);

                    }
                    else
                    {
                        e.Graphics.DrawText(Convert.ToInt32(currVeh.Speed * SpeedMultiplier).ToString(), dashBoardLocation.X + 0.06f, dashBoardLocation.Y + 0.03f);
                        e.Graphics.DrawText((SpeedMultiplier == 3.6f) ? "KPH" : "MPH", dashBoardLocation.X + 0.09f, dashBoardLocation.Y + 0.03f);
                    }
                }
            }
            catch (Exception crap)
            {
                Log("carScript_PerFrameDrawing", crap.Message);
            }
        }
        /// <summary>
        /// run every frame, digitalMode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void carScript_PerFrameDrawing_classicMode(object sender, GTA.GraphicsEventArgs e)
        {
            try
            {
                if (reFuel)
                {
                    GTA.Native.Function.Call("FORCE_CAR_LIGHTS", currVeh, 0);
                    currVeh.EngineRunning = false;
                }

                e.Graphics.Scaling = FontScaling.ScreenUnits;

                if (Player.Character.isInVehicle())
                {
                    try
                    {
                        e.Graphics.DrawRectangle(
                            new RectangleF(dashBoardLocation.X - 0.0035f, dashBoardLocation.Y - 0.004f, GaugeWidth, 0.0125f),
                            GTA.ColorIndex.Black);
                        e.Graphics.DrawRectangle(
                            new RectangleF(dashBoardLocation.X, dashBoardLocation.Y, (1 * (GaugeWidth - 0.007f)) / 1, 0.006f),
                            (GTA.ColorIndex)1);

                        e.Graphics.DrawRectangle(
                                new RectangleF(dashBoardLocation.X, dashBoardLocation.Y, (currVeh.Metadata.Fuel * (GaugeWidth - 0.007f)) / currVeh.Metadata.MaxTank, 0.006f),
                                (currVeh.Metadata.Fuel <= currVeh.Metadata.Reserve) ? ((flash < 5) ? (GTA.ColorIndex)1 : (GTA.ColorIndex)35) : (GTA.ColorIndex)50);

                        flash = (flash == 20) ? 0 : flash + 1;
                    }
                    catch
                    {

                    }
                }
            }
            catch (Exception crap)
            {
                Log("carScript_PerFrameDrawing", crap.Message);
            }
        }
        #endregion
    }
}
