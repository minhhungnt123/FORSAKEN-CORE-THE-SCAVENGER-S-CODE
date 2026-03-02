using UnityEngine;
using System.Collections;

namespace Assets._Scripts._Modules
{
	public class ChassicModule: RobotModule
	{

        protected override void Awake()
        {
            base.Awake();
            moduleName = "Khung gầm trung tâm";
        }
    }
}