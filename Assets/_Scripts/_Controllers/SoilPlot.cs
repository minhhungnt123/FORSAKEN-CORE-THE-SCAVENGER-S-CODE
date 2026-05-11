using UnityEngine;

// Định nghĩa Interface cho các trạng thái của ô đất
public interface ISoilState
{
    string GetPrompt();
    void Interact(SoilPlot plot);
}

public class SoilPlot : MonoBehaviour, IInteractable
{
    private ISoilState currentState;
    public int gridX { get; private set; }
    public int gridZ { get; private set; }

    // Các tham chiếu model/prefab cây trồng (để xử lý visual scaling sau này)
    public GameObject plantModel;

    private void Awake()
    {
        // Khởi tạo trạng thái ban đầu là Empty
        SetState(new EmptyState());
    }

    public void Init(int x, int z)
    {
        gridX = x;
        gridZ = z;
        gameObject.name = $"SoilPlot_{x}_{z}";
    }

    public void SetState(ISoilState newState)
    {
        currentState = newState;
    }

    // --- Triển khai IInteractable ---
    public string GetInteractPrompt()
    {
        return currentState.GetPrompt();
    }

    public void Interact()
    {
        currentState.Interact(this);
    }
}

// ================= CÁC TRẠNG THÁI (STATES) =================

public class EmptyState : ISoilState
{
    public string GetPrompt() => "Trồng hạt giống [E]";

    public void Interact(SoilPlot plot)
    {
        Debug.Log("Đã gieo hạt!");
        // Thêm logic: sinh ra model mầm cây non ở đây
        // Chuyển sang trạng thái Planted
        plot.SetState(new PlantedState());
    }
}

public class PlantedState : ISoilState
{
    public string GetPrompt() => "Tưới nước [E]"; // Hoặc hiển thị % phát triển

    public void Interact(SoilPlot plot)
    {
        Debug.Log("Tưới nước xong, cây phát triển nhanh hơn!");
        // Thêm logic: Khi thời gian chạy đủ hoặc tưới đủ nước, cây lớn lên.
        // Tạm thời giả lập nhấn E lần nữa thì thành Grown
        plot.SetState(new GrownState());
    }
}

public class GrownState : ISoilState
{
    public string GetPrompt() => "Thu hoạch [E]";

    public void Interact(SoilPlot plot)
    {
        Debug.Log("Thu hoạch nông sản!");
        // Thêm logic: Cộng tiền/vật phẩm vào túi đồ người chơi, xóa model cây
        plot.SetState(new EmptyState());
    }
}