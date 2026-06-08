using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    public Component targetComponent;

    private void Awake()
    {
        var objects = FindObjectsByType<Component>(FindObjectsSortMode.None);

        int count = 0;

        foreach (var obj in objects)
        {
            if (obj.GetType() == targetComponent.GetType())
                count++;
        }

        if (count > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}