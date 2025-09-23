namespace Fortnite_Replay_Parser_GUI.Templates
{
    internal class Template_MatchResult
    {
        public static string MatchStatTemplate = """
            ========= Match Stats ============
            Started : {{ started_at }}
            Ended : {{ ended_at }}
            Duration : {{ duration }}
            Total Players: {{ total_players }}(Humans : {{ human_players }} / Bots : {{ bot_players }})
            {{ player_result }}
            {{ system_info }}

            (This output has been provided by: https://github.com/Kumapapa2012/Fortnite_Replay_Parser_GUI/)
            """;

        public static string PlayerResultTemplate = """
            ========= Player Results =========
            {{ for elim in eliminations }}
            {{ fn_form_number elim.index }}: {{ elim.time }} - {{ elim.player_name }}({{ if elim.is_bot }}bot{{ else }}human{{ end }}){{ end }}
            {{ if eliminated }}
            >>>> {{ player_name }} was eliminated by {{ eliminated.player_name }}({{ if eliminated.is_bot }}bot{{ else }}human{{ end }}) at {{ eliminated.time }} (Placement: {{ fn_form_number placement }})
            {{ else }}
            {{ if placement ==1 }}>>>> {{ player_name }} won the game!
            {{ else }}>>>> The replay ended before the player was eliminated(or someone won the game).
            {{ end }}
            {{ end }}
            """;

        public static string SystemInfoTemplate = """
            ========= Platform ===============
            OS : {{ os }}
            CPU : {{ cpu }}
            Memory : {{ memory }} - {{ available_memory }}
            GPU : {{ gpu }}
            Resolution : {{ resolution }}
            """;
    }
}
