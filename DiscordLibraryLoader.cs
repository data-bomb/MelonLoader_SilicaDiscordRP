using MelonLoader;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DiscordStatus
{
    public static class DiscordLibraryLoader
    {
        public static bool Loaded { get; private set; }

        public const string LibPath86 = "Si_DiscordStatus.Dependencies.x86.discord_game_sdk.dll";
        public const string LibPath64 = "Si_DiscordStatus.Dependencies.x86_64.discord_game_sdk64.dll";
        public const string LibDestinationPath = "UserData/DiscordStatus/discord_game_sdk.dll";

        public static void LoadLibrary()
        {
            if (Loaded)
                return;

            if (!File.Exists(LibDestinationPath))
            {
                #pragma warning disable CS8604
                Directory.CreateDirectory(Path.GetDirectoryName(LibDestinationPath));
                #pragma warning restore CS8604
                using (var libStr = Assembly.GetExecutingAssembly().GetManifestResourceStream(MelonUtils.IsGame32Bit() ? LibPath86 : LibPath64))
                {
                    if (libStr == null)
                        throw new NullReferenceException($"Could not find the SDK library resource.");

                    var data = new byte[libStr.Length];
                    libStr.Read(data, 0, data.Length);
                    File.WriteAllBytes(LibDestinationPath, data);
                }
            }

            if (LoadLibrary(LibDestinationPath) == IntPtr.Zero)
                throw new DllNotFoundException($"Could not load the Discord SDK library from '{LibDestinationPath}'.");

            MelonLogger.Msg("Discord Library loaded.");
            Loaded = true;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);
    }
}
