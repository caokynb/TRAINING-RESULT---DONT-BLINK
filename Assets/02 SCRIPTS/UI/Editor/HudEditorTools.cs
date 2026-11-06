#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class HudEditorTools
{
    [MenuItem("Tools/Backrooms/Hiển thị toàn bộ HUD trong Scene (Edit Mode)", false, 10)]
    public static void ShowAllHudInScene()
    {
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            GameObject pObj = GameObject.Find("Player");
            if (pObj != null) player = pObj.GetComponent<PlayerMovement>();
        }

        if (player == null)
        {
            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy Player trong Scene hiện tại!\nHãy mở Scene Level01_Art có chứa Player để thiết lập.", "OK");
            return;
        }

        // 1. Blink HUD (Thanh mỏi mắt, Vignette)
        BlinkHudView blinkHud = player.GetComponent<BlinkHudView>();
        if (blinkHud != null)
        {
            blinkHud.SetupHudInEditor();
        }

        // 2. Interaction HUD (Tâm ngắm, Bảng phím nhặt/mở cửa, Huy hiệu chìa khóa góc màn hình)
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor != null)
        {
            interactor.SetupUIInEditor();
        }

        Selection.activeGameObject = player.gameObject;
        EditorUtility.SetDirty(player.gameObject);
        Debug.Log("[HudEditorTools] Đã tạo và hiển thị toàn bộ HUD trực tiếp dưới Player trong Hierarchy! Bạn có thể căn chỉnh vị trí / kích thước / màu sắc ngay trong Scene View và Game View.");
    }

    [MenuItem("Tools/Backrooms/Ẩn Preview Gợi Ý & Chìa Khóa (Sẵn sàng Play)", false, 11)]
    public static void HidePreviewHud()
    {
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;

        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor != null)
        {
            interactor.HidePreviewInEditor();
        }

        TutorialScreenView tutorial = player.GetComponent<TutorialScreenView>();
        if (tutorial != null)
        {
            tutorial.HideTutorialInEditor();
        }

        EditorUtility.SetDirty(player.gameObject);
        Debug.Log("[HudEditorTools] Đã ẩn preview Prompt và Huy hiệu chìa khóa. Sẵn sàng Play!");
    }

    [MenuItem("Tools/Backrooms/Hiển thị Preview Màn hình Hướng Dẫn (Tutorial)", false, 12)]
    public static void ShowTutorialPreview()
    {
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;

        TutorialScreenView tutorial = player.GetComponent<TutorialScreenView>();
        if (tutorial != null)
        {
            tutorial.SetupTutorialInEditor();
        }
    }
}
#endif
