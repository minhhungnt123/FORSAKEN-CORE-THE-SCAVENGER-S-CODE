using UnityEngine;
using System.Collections;

namespace Assets._Scripts._Modules
{
	public class ChassicModule: RobotModule
	{
        // Awake được gọi khi module này được khởi tạo
        protected override void Awake()
        {
            base.Awake();
            moduleName = "Khung gầm trung tâm";
        }
    }
}