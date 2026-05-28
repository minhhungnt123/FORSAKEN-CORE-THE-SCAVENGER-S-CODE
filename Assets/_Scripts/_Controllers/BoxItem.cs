using UnityEngine;
using RoboticsProject.Interfaces;

public class BoxItem : MonoBehaviour, IInteractable
{
    // Cung cấp chữ cho UI hiện lên khi Player đứng gần
    public string GetInteractPrompt()
    {
        return "Nhặt chiếc hộp [E]";
    }

    // Logic xử lý khi Player bấm E
    public void OnInteract()
    {
        Debug.Log($"Bạn đã nhặt chiếc hộp: {gameObject.name}");

        // Code nhặt đồ, ví dụ: 
        // Ẩn object đi hoặc Destroy nó
        Destroy(gameObject);
    }
}