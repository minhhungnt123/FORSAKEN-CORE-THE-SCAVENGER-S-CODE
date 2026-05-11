using UnityEngine;

public interface IInteractable
{
    // Hàm này trả về một chuỗi mô tả hành động tương tác
    string GetInteractPrompt();
    // Hàm này được gọi khi người chơi thực hiện hành động tương tác
    void Interact();
}   