using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneBootstrap : MonoBehaviour
{
    [SerializeField] private string[] additiveSceneNames =
    {
        "Level01_Art",
        "Level01_Systems",
        "Level01_Monster"
    };
    [SerializeField] private string spawnSceneName = "Level01_Art";
    [SerializeField] private string playerSpawnTag = "PlayerSpawn";
    [SerializeField] private GameObject playerPrefab;

    private IEnumerator Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("SceneBootstrap: Assign the Player prefab in the Inspector.", this);
            yield break;
        }

        foreach (string sceneName in additiveSceneNames)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                continue;

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.isLoaded)
                continue;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"SceneBootstrap: '{sceneName}' is missing from the active Build Profile Scene List.", this);
                yield break;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogError($"SceneBootstrap: Could not start loading '{sceneName}'.", this);
                yield break;
            }

            yield return loadOperation;
        }

        if (!SceneManager.SetActiveScene(gameObject.scene))
        {
            Debug.LogError("SceneBootstrap: Could not set Main as the active scene.", this);
            yield break;
        }

        Scene spawnScene = SceneManager.GetSceneByName(spawnSceneName);
        Transform spawnMarker = FindSpawnMarker(spawnScene);
        if (spawnMarker == null)
        {
            Debug.LogError(
                $"SceneBootstrap: No active object with tag '{playerSpawnTag}' was found in '{spawnSceneName}'.", this);
            yield break;
        }

        if (Object.FindFirstObjectByType<PlayerInputReader>() != null)
        {
            Debug.LogWarning("SceneBootstrap: A Player already exists; skipping duplicate spawn.", this);
            yield break;
        }

        GameObject player = Instantiate(playerPrefab, spawnMarker.position, spawnMarker.rotation);
        player.name = playerPrefab.name;
        DisableOtherCamerasAndAudioListeners(player.transform);
    }

    private Transform FindSpawnMarker(Scene scene)
    {
        if (!scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.gameObject.activeInHierarchy && candidate.CompareTag(playerSpawnTag))
                    return candidate;
            }
        }

        return null;
    }

    private static void DisableOtherCamerasAndAudioListeners(Transform playerRoot)
    {
        foreach (Camera sceneCamera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (sceneCamera.transform != playerRoot && !sceneCamera.transform.IsChildOf(playerRoot))
                sceneCamera.enabled = false;
        }

        foreach (AudioListener listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (listener.transform != playerRoot && !listener.transform.IsChildOf(playerRoot))
                listener.enabled = false;
        }
    }
}
