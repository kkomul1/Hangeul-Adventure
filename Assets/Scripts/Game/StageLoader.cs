using System;
using System.Collections.Generic;
using HangeulAdventure.Engine;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 스테이지 JSON 포맷 (Resources/Stages/*.json):
    /// {
    ///   "id": 1, "title": "첫 글자",
    ///   "rows": ["ㄱ.ㅏ", "#.."],        // 위→아래. '.'=빈칸, '#'=없는 칸
    ///   "goals": [{"display":"가", "slots":"가"}],
    ///   "minMoves": 3,                    // 솔버 검증값 (루비 별 기준)
    ///   "stars": [0, 5, 4]                // [1별, 2별, 3별] 이동 수 상한. 0=기본 공식 사용
    /// }
    /// </summary>
    [Serializable]
    public class StageJson
    {
        public int id;
        public string title = "";
        public string[] rows;
        public string[] pins;      // 사슬 칸 (D-24): rows와 같은 크기, '!' = 사슬. 생략 가능
        public GoalJson[] goals;
        public int minMoves;
        public int[] stars;
        public int difficulty = 1; // 1~10, 생략 시 1
        public string hint = "";   // 튜토리얼 안내 문구
    }

    [Serializable]
    public class GoalJson
    {
        public string display = "";
        public string slots = "";
        public string clue = "";   // 뜻풀이 목표 (M3-3): 있으면 정답 글자를 숨기고 이 문구 표시
    }

    public static class StageLoader
    {
        public const string ResourceFolder = "Stages";

        public static StageData FromJson(string json)
        {
            var sj = JsonUtility.FromJson<StageJson>(json);
            if (sj == null || sj.rows == null || sj.rows.Length == 0)
                throw new ArgumentException("스테이지 JSON 형식 오류: rows 없음");

            var groups = new GoalGroup[sj.goals?.Length ?? 0];
            for (int i = 0; i < groups.Length; i++)
            {
                var g = sj.goals[i];
                groups[i] = new GoalGroup
                {
                    display = string.IsNullOrEmpty(g.display) ? g.slots : g.display,
                    slots = g.slots.ToCharArray(),
                    clue = g.clue ?? "",
                };
            }

            var stage = StageBuilder.FromRows(sj.rows, sj.pins, groups);
            stage.id = sj.id;
            stage.title = sj.title ?? "";
            stage.minMoves = sj.minMoves;
            stage.difficulty = Mathf.Clamp(sj.difficulty, 1, 5);
            stage.hint = sj.hint ?? "";
            stage.starThresholds = (sj.stars != null && sj.stars.Length == 3 && sj.stars[2] > 0)
                ? sj.stars
                : StageData.DefaultStarThresholds(sj.minMoves);
            if (stage.starThresholds[0] <= 0) stage.starThresholds[0] = int.MaxValue;
            return stage;
        }

        /// <summary>내 스테이지 (persistentDataPath/CustomStages)를 id 순으로 로드.</summary>
        public static List<StageData> LoadCustom()
        {
            var list = new List<StageData>();
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "CustomStages");
            if (!System.IO.Directory.Exists(dir)) return list;
            foreach (string path in System.IO.Directory.GetFiles(dir, "*.json"))
            {
                try { list.Add(FromJson(System.IO.File.ReadAllText(path))); }
                catch (Exception e) { Debug.LogError($"내 스테이지 로드 실패 ({System.IO.Path.GetFileName(path)}): {e.Message}"); }
            }
            list.Sort((a, b) => a.id.CompareTo(b.id));
            return list;
        }

        /// <summary>Resources/Stages의 모든 스테이지를 id 순으로 로드.</summary>
        public static List<StageData> LoadAll()
        {
            var assets = Resources.LoadAll<TextAsset>(ResourceFolder);
            var list = new List<StageData>(assets.Length);
            foreach (var a in assets)
            {
                try { list.Add(FromJson(a.text)); }
                catch (Exception e) { Debug.LogError($"스테이지 로드 실패 ({a.name}): {e.Message}"); }
            }
            list.Sort((a, b) => a.id.CompareTo(b.id));
            for (int i = 1; i < list.Count; i++)
                if (list[i].id == list[i - 1].id)
                    Debug.LogError($"스테이지 id 중복: {list[i].id} — 진행 저장이 섞입니다");
            return list;
        }
    }
}
