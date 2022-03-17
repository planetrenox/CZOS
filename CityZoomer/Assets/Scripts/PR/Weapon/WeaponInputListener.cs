using PR.Weapon.Util;
using UnityEngine;

namespace PR.Weapon.Weapon
{
    public class WeaponInputListener : MonoBehaviour
    {
        public static GameObject w011GameObject;
        public static Animator w011AnimController;
        public static Transform cameraSightTransform;
        
        private float scrollInput;
        private static readonly int MousePrimary = Animator.StringToHash("MousePrimary");
        private static readonly int MouseSecondary = Animator.StringToHash("MouseSecondary");
        private int mouseWheelValue = 0;
        
        private void Start()
        {
            w011GameObject = GameObject.Find("w011_double_axe");
            w011AnimController = w011GameObject.GetComponent<Animator>();
            cameraSightTransform = GameObject.Find("CameraSight").transform;
            W011Controller.disablew011();
        }
        
        private void Update()
        {
            //// SCROLLWHEEL INPUT | WEAPON SWITCH
            scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                mouseWheelValue += Mathf.RoundToInt((scrollInput * 10)); // Make scroll wheel value cycle through ints & hold value
                if (mouseWheelValue == 1 )
                {
                    W011Controller.enablew011();
                }
                else
                {
                    W011Controller.disablew011();
                }
            }
            
            if (mouseWheelValue == 1)
            {
                // MOUSE 0 INPUT
                w011AnimController.SetBool(MousePrimary, Input.GetKey(KeyCode.Mouse0));

                // MOUSE 1 INPUT
                w011AnimController.SetBool(MouseSecondary, Input.GetKey(KeyCode.Mouse1));
            }
            
        }
        
        // in start put //playerAiming = cameraSight.GetComponent<PlayerAiming>(); 
        //private PlayerAiming playerAiming;
        //testing
        // public void AimPunch(int x, int y)
        // {
        //     playerAiming.ViewPunch(new Vector2(x, y));
        // }
    
    }
}
