using UnityEditor;
using UnityEngine;

namespace HangeulAdventure.EditorTools
{
    /// <summary>
    /// 코드 생성 UI를 프리팹으로 추출하는 도구 (M3-2, D-11 전환).
    /// 플레이 모드에서 실행하면 현재 화면에 살아 있는 UI 루트를 Resources/UiPrefabs/{이름}.prefab으로 저장한다.
    /// 프리팹이 존재하면 해당 화면은 다음 실행부터 코드 생성 대신 프리팹을 인스턴스화해 쓴다
    /// (현재 타이틀 화면 지원 — GameApp.Title.cs의 BuildTitleFromPrefab 참조).
    /// 주의: 절차 생성 스프라이트(RoundedSprite)는 에셋이 아니라 프리팹에 저장되지 않음 —
    /// 런타임 바인딩 시 sprite==null인 Image에 자동 복원된다.
    /// </summary>
    public static class UiPrefabTools
    {
        private const string Dir = "Assets/Resources/UiPrefabs";

        // 추출 대상 UI 루트 (씬에서 이 이름의 활성 오브젝트를 찾음)
        private static readonly string[] Roots = { "TitlePanel", "SelectPanel", "SettingsPopup", "BattlePanel", "MapHud", "Hud" };

        [MenuItem("HangeulAdventure/UI 프리팹 추출 (플레이 중 현재 화면)")]
        public static void Extract()
        {
            Debug.Log(ExtractInternal());
        }

        public static string ExtractInternal()
        {
            if (!Application.isPlaying)
                return "플레이 모드에서 실행하세요 — UI가 런타임에 생성됩니다.";

            if (!AssetDatabase.IsValidFolder(Dir))
            {
                System.IO.Directory.CreateDirectory(Dir);
                AssetDatabase.Refresh();
            }

            var report = new System.Text.StringBuilder();
            int saved = 0;
            foreach (string name in Roots)
            {
                var go = GameObject.Find(name); // 활성 오브젝트만 — 현재 화면에 보이는 것을 추출
                if (go == null) continue;
                string path = $"{Dir}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, path);
                report.AppendLine($"저장: {path}");
                saved++;
            }
            report.Append(saved == 0
                ? "추출할 UI가 없습니다 (해당 화면을 띄운 상태에서 실행)."
                : $"완료: {saved}개. 프리팹이 있으면 다음 실행부터 프리팹을 사용합니다 (현재 타이틀 지원).");
            return report.ToString();
        }
    }
}
