using UnityEngine;

public interface IConnectable
{
    void Connect(Transform parentSocket);
    void Disconnect();
}

// Module biet minh can socket nao de ChassisModule dinh tuyen dung slot
public interface IAssemblable
{
    string RequiredSocketTag { get; }
    // OnAssembled va OnDetached la virtual method trong RobotModule
    // khong bat buoc qua interface de tranh loi ten method
}