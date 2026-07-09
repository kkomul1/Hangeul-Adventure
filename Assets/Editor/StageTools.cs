using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HangeulAdventure.Engine;
using HangeulAdventure.Game;
using UnityEditor;
using UnityEngine;

namespace HangeulAdventure.EditorTools
{
    /// <summary>
    /// 스테이지 제작 지원 도구: 모든 스테이지 JSON을 솔버로 검증하고
    /// minMoves를 실측값으로 자동 기입한다 (명세 10장 파이프라인).
    /// </summary>
    public static class StageTools
    {
        private const string Folder = "Assets/Resources/Stages";

        [MenuItem("HangeulAdventure/스테이지 minMoves 자동 기입 및 검증")]
        public static void SolveAndWriteAll()
        {
            Debug.Log(SolveAndWriteAllInternal());
        }

        /// <summary>execute_code에서 호출 가능하도록 결과를 문자열로 반환.</summary>
        public static string SolveAndWriteAllInternal()
        {
            var report = new StringBuilder();
            int ok = 0, updated = 0, unsolvable = 0, aborted = 0;

            foreach (string path in Directory.GetFiles(Folder, "stage_*.json"))
            {
                string json = File.ReadAllText(path);
                StageData stage;
                try { stage = StageLoader.FromJson(json); }
                catch (System.Exception e)
                {
                    report.AppendLine($"[파싱실패] {Path.GetFileName(path)}: {e.Message}");
                    unsolvable++;
                    continue;
                }

                var r = Solver.Solve(stage);
                if (r.Aborted)
                {
                    report.AppendLine($"[상태폭발] {Path.GetFileName(path)} (탐색 {r.ExploredStates}) — 단순화 필요");
                    aborted++;
                    continue;
                }
                if (!r.Solvable)
                {
                    report.AppendLine($"[풀이불가] {Path.GetFileName(path)} ({stage.title})");
                    unsolvable++;
                    continue;
                }

                if (stage.minMoves != r.MinMoves)
                {
                    string newJson = Regex.Replace(json, "\"minMoves\"\\s*:\\s*\\d+", $"\"minMoves\": {r.MinMoves}");
                    File.WriteAllText(path, newJson);
                    report.AppendLine($"[기입] {Path.GetFileName(path)} ({stage.title}): minMoves {stage.minMoves} → {r.MinMoves}");
                    updated++;
                }
                else ok++;
            }

            AssetDatabase.Refresh();
            report.AppendLine($"결과: 일치 {ok}, 기입 {updated}, 풀이불가 {unsolvable}, 상태폭발 {aborted}");
            return report.ToString();
        }
    }
}
