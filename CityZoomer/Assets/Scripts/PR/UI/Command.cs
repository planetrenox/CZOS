using System;
using Fragsurf.Movement;
using UnityEngine;


namespace PR
{
    public class Command : MonoBehaviour
    {
        static string s_ConsoleTrace = "";
        static string s_myCommand = "";
        private string output;
        private string stack;
        private string value;
        public static bool s_sandbox = false;
        private static bool s_isVisiable = false;
        [NonSerialized] protected Rect consoleRect;

        private void Awake()
        {
            consoleRect = new Rect(Screen.width - 460, Screen.height - 300, 450, 290);
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(UI.Button_Console_READ)) s_isVisiable = !s_isVisiable;
        }

        public void Log(string logString, string stackTrace, LogType type)
        {
            var time = System.DateTime.Now.ToShortTimeString() + ": ";
            output = logString;
            stack = stackTrace;
            s_ConsoleTrace = s_ConsoleTrace + time + output + "\n";
            if (s_ConsoleTrace.Length > 5000) s_ConsoleTrace = s_ConsoleTrace.Substring(0, 4000);
        }

        public static void LocalLog(string val)
        {
            s_ConsoleTrace = s_ConsoleTrace + val + "\n";
        }

        public static void PrintFailure_SteamAPI()
        {
            LocalLog("Steam is down or you are offline. Please restart Steam & try again.");
        }


        private void OnGUI()
        {
            if (!s_isVisiable) return;
            if (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.KeyUp)
            {
                var commands = s_myCommand.Split(';');
                foreach (var command in commands)
                {
                    var commandSplit = command.Split(' ');
                    if (commandSplit.Length == 1 && commandSplit[0].Equals("clear"))
                    {
                        s_ConsoleTrace = "";
                        s_myCommand = "";
                        return;
                    }
                    if (commandSplit.Length != 2)
                    {
                        LocalLog("need value.");
                        continue;
                    }
                    CallStaticMethod(commandSplit[0], commandSplit[1]);
                }

                s_ConsoleTrace = s_ConsoleTrace + s_myCommand + "\n";
                s_myCommand = "";
            }


            consoleRect = GUI.Window(0, consoleRect, ConsoleWindow, "Con");
        }


        void ConsoleWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            GUI.TextArea(new Rect(0, 20, 450, 250), s_ConsoleTrace);
            s_myCommand = GUI.TextField(new Rect(0, 270, 450, 20), s_myCommand);
        }

        protected void CallStaticMethod(string command, string value)
        {
            this.value = value;
            Invoke(command, 0f);
        }


        #region Commands

        private void debug()
        {
            switch (value)
            {
                case "0":
                    LocalLog("saved until session end.");
                    Application.logMessageReceived -= Log;
                    break;
                case "1":
                    LocalLog("saved until session end.");
                    Application.logMessageReceived += Log;
                    break;
                default:
                    LocalLog("value out of range.");
                    break;
            }
        }

        private void args()
        {
            var args = System.Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                LocalLog(arg);
            }
        }

        private void god()
        {
            switch (value)
            {
                case "0":
                    LocalLog("saved until session end.");
                    SurfCharacter.god = false;
                    break;
                case "1":
                    LocalLog("saved. score will not be saved until session end.");
                    s_sandbox = true;
                    SurfCharacter.god = true;
                    break;
                default:
                    LocalLog("value out of range.");
                    break;
            }
        }

        private void rawinput()
        {
            switch (value)
            {
                case "0":
                    LocalLog("saved until session end.");
                    PlayerAiming.isSmoothingMouse = true;
                    break;
                case "1":
                    LocalLog("saved until session end.");
                    PlayerAiming.isSmoothingMouse = false;
                    break;
                default:
                    LocalLog("value out of range.");
                    break;
            }
        }

        private void airaccel()
        {
            if (float.TryParse(value, out float newAccel))
            {
                LocalLog("saved. score will not be saved until session end.");
                s_sandbox = true;
                MovementConfig.airAcceleration = newAccel;
            }

            else LocalLog("Number required for value.");
        }

        private void jumpforce()
        {
            if (float.TryParse(value, out float newJumpforce))
            {
                LocalLog("saved. score will not be saved until session end.");
                s_sandbox = true;
                MovementConfig.jumpForce = newJumpforce;
            }

            else LocalLog("Number required for value.");
        }

        private void velocitycap()
        {
            if (float.TryParse(value, out float newvelocitycap))
            {
                LocalLog("saved. score will not be saved until session end.");
                s_sandbox = true;
                MovementConfig.maxVelocity = newvelocitycap;
            }

            else LocalLog("Number required for value.");
        }

        private void gravity()
        {
            if (float.TryParse(value, out float newGravity))
            {
                LocalLog("saved. score will not be saved until session end.");
                s_sandbox = true;
                MovementConfig.gravity = newGravity;
            }

            else LocalLog("Number required for value.");
        }

        #endregion
    }
}