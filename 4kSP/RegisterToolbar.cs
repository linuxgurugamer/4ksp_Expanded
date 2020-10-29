

using ToolbarControl_NS;
using UnityEngine;

namespace FourkSP
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar2 : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(FourkSP.MODID, FourkSP.MODNAME);
        }
    }

}
