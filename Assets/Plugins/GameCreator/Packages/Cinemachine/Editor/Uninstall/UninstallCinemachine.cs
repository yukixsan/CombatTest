using UnityEditor;

namespace GameCreator.Editor.Installs
{
    public static class UninstallCinemachine
    {
        [MenuItem(
            itemName: "Game Creator/Uninstall/Cinemachine",
            isValidateFunction: false,
            priority: UninstallManager.PRIORITY
        )]
        
        private static void Uninstall()
        {
            UninstallManager.Uninstall("Cinemachine");
        }
    }
}