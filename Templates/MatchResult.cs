using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fortnite_Replay_Parser_GUI.Templates
{
    internal class MatchResult
    {
        public static string MatchStatTemplate = """
            ======== Match Stats =========
            Started : {{ started_at }}
            Ended : {{ ended_at }}
            Duration : {{ duration }}
            Total Players: {{ total_players }}(Humans : {{ human_players }} / Bots : {{ bot_players }})
            {{ player_result }}
            {{ system_info }}
            =============================
            (This output has been provided by: https://github.com/Kumapapa2012/Fortnite_Replay_Parser_GUI/)
            """;

        public static string PlayerResultTemplate = """
            =============================
            Player: {{ player_name }}
            =============================
            {{ for elim in eliminations }}
            {{ fn_form_number elim.index }}: {{ elim.time }} - {{ elim.player_name }}({{ if elim.is_bot }}bot{{ else }}human{{ end }}){{ end }}
            {{ if eliminated }}
            {{ player_name }} was eliminated by {{ eliminated.player_name }}({{ if eliminated.is_bot }}bot{{ else }}human{{ end }}) at {{ eliminated.time }}
            {{ else }}
            ==== {{ player_name }} got a Victory Royale!! ====
            {{ end }}
            =============================            
            """;
    }
}
