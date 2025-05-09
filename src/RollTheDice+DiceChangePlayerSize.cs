using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        public override string ModuleName => "RollTheDice";
        public override string ModuleVersion => "1.0.0";

        private List<CCSPlayerPawn> _playersWithChangedModelSize = new();

        public override void OnChatMessage(CCSPlayerController sender, ChatMessageEventArgs args)
        {
            var message = args.Message.Trim();
            if (!message.StartsWith("!givedice", StringComparison.OrdinalIgnoreCase))
                return;

            var parts = message.Split(' ');
            if (parts.Length != 4 || parts[2].ToLower() != "size")
            {
                sender.PrintToChat("Použití: !givedice <JmenoHrace> size <velikost>");
                return;
            }

            string targetName = parts[1];
            if (!float.TryParse(parts[3], out float size))
            {
                sender.PrintToChat("Neplatná velikost.");
                return;
            }

            var targetPlayer = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.PlayerName.Equals(targetName, StringComparison.OrdinalIgnoreCase));
            if (targetPlayer == null || targetPlayer.PlayerPawn == null || !targetPlayer.PlayerPawn.IsValid)
            {
                sender.PrintToChat($"Hráč '{targetName}' nebyl nalezen nebo není validní.");
                return;
            }

            var result = DiceChangePlayerSize(targetPlayer, targetPlayer.PlayerPawn.Value, size);
            sender.PrintToChat($"✅ Změněna velikost hráče {result["playerName"]} na {result["playerSize"]}");
        }

        private Dictionary<string, string> DiceChangePlayerSize(CCSPlayerController player, CCSPlayerPawn playerPawn, float playerSize)
        {
            _playersWithChangedModelSize.Add(playerPawn);

            var playerSceneNode = playerPawn.CBodyComponent?.SceneNode;
            if (playerSceneNode == null)
                return new Dictionary<string, string>
                {
                    {"error", "command.rollthedice.error"}
                };

            playerSceneNode.GetSkeletonInstance().Scale = playerSize;
            playerPawn.AcceptInput("SetScale", null, null, playerSize.ToString());
            Server.NextFrame(() =>
            {
                if (playerPawn == null) return;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
            });

            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName },
                { "playerSize", playerSize.ToString() }
            };
        }

        private void DiceChangePlayerSizeUnload()
        {
            DiceChangePlayerSizeReset();
        }

        private void DiceChangePlayerSizeReset()
        {
            List<CCSPlayerPawn> _playersWithChangedModelSizeCopy = new(_playersWithChangedModelSize);
            foreach (CCSPlayerPawn playerPawn in _playersWithChangedModelSizeCopy)
            {
                try
                {
                    if (playerPawn == null) continue;
                    var playerSceneNode = playerPawn.CBodyComponent?.SceneNode;
                    if (playerSceneNode == null) continue;
                    playerSceneNode.GetSkeletonInstance().Scale = 1.0f;
                    playerPawn.AcceptInput("SetScale", null, null, "1.0");
                    Server.NextFrame(() =>
                    {
                        if (playerPawn == null) return;
                        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
                    });
                }
                catch
                {
                    // do nothing
                }
            }
            _playersWithChangedModelSize.Clear();
        }

        private void DiceChangePlayerSizeResetForPlayer(CCSPlayerController player)
        {
            if (player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return;
            if (!_playersWithChangedModelSize.Contains(player.PlayerPawn.Value)) return;
            var playerSceneNode = player.PlayerPawn.Value.CBodyComponent?.SceneNode;
            if (playerSceneNode == null) return;
            playerSceneNode.GetSkeletonInstance().Scale = 1.0f;
            player.PlayerPawn.Value.AcceptInput("SetScale", null, null, "1.0");
            Server.NextFrame(() =>
            {
                if (player.PlayerPawn.Value == null) return;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_CBodyComponent");
            });
            _playersWithChangedModelSize.Remove(player.PlayerPawn.Value);
        }
    }
}
