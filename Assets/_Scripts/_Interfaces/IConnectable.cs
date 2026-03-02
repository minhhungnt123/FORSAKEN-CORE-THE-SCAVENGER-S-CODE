using UnityEngine;

public interface IConnectable
{
    // Hàm gọi khi module này hít vào một socket của module khác
    void Connect(Transform parentSocket);

    // Hàm gọi khi người chơi tháo module ra
    void Disconnect();
}