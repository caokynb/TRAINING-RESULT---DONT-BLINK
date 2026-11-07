using UnityEditor;
using UnityEngine;

public static class MainMenuMapBuilder
{
    private const string MainMenuMapPath = "Assets/03 ASSETS/Maps/mainmenu_map.txt";
    private const string ThemePath = "Assets/07 SETTINGS/BackroomsTheme.asset";

    [MenuItem("Tools/Backrooms/Build Main Menu Map")]
    public static void BuildMainMenuSceneMap()
    {
        // 0. Force refresh asset file text để nạp đúng bản sửa lỗi
        AssetDatabase.ImportAsset(MainMenuMapPath, ImportAssetOptions.ForceUpdate);

        // 1. Tìm hoặc tạo MapGrid trong scene MainMenu
        MapGrid mapGrid = Object.FindFirstObjectByType<MapGrid>();
        if (mapGrid == null)
        {
            GameObject gridObj = new GameObject("MapGrid");
            Undo.RegisterCreatedObjectUndo(gridObj, "Create MapGrid");
            mapGrid = gridObj.AddComponent<MapGrid>();
        }

        // 2. Gán map file mainmenu_map.txt
        TextAsset menuMap = AssetDatabase.LoadAssetAtPath<TextAsset>(MainMenuMapPath);
        if (menuMap == null)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy file map tại: {MainMenuMapPath}", "OK");
            return;
        }
        mapGrid.LevelFile = menuMap;

        // 3. Gán theme BackroomsTheme
        MapTheme theme = AssetDatabase.LoadAssetAtPath<MapTheme>(ThemePath);
        if (theme != null)
        {
            mapGrid.Theme = theme;
        }

        EditorUtility.SetDirty(mapGrid);

        // 4. Chạy Build Map
        MapBuilder.BuildMap();

        // 5. Main Menu không cần MonsterSpawner, xóa nếu được sinh ra
        MonsterSpawner spawner = Object.FindFirstObjectByType<MonsterSpawner>();
        if (spawner != null)
        {
            Undo.DestroyObjectImmediate(spawner.gameObject);
        }

        // 6. Căn chỉnh Camera chuẩn góc phối cảnh Portal 2 (Hành lang bên trái, đèn chớp bên phải)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Undo.RecordObject(mainCam.transform, "Align Main Menu Camera");
            mainCam.transform.position = new Vector3(7.5f, 1.6f, 7.5f);
            mainCam.transform.rotation = Quaternion.Euler(3f, 25f, 0f);
            mainCam.fieldOfView = 70f;

            if (mainCam.GetComponent<AudioListener>() == null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
            }

            EditorUtility.SetDirty(mainCam);
        }

        EditorUtility.DisplayDialog(
            "Build Main Menu Map",
            "Đã tạo không gian 3D cho Main Menu và tự động căn góc Camera chuẩn phong cách Portal 2!\n\nNhấn Play để xem ánh đèn chớp tắt bên phải.",
            "Tuyệt vời"
        );
    }
}
