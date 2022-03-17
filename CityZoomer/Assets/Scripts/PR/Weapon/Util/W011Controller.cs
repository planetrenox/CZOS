using PR.Weapon.Weapon;
using UnityEngine;

namespace PR.Weapon.Util
{
    public static class W011Controller
    {
        private static readonly int Equip = Animator.StringToHash("Equip");

        public static void enablew011()
        {
            WeaponInputListener.w011GameObject.SetActive(true);
            WeaponInputListener.w011AnimController.SetBool(Equip, true);
        }
        public static void disablew011()
        {
            WeaponInputListener.w011AnimController.SetBool(Equip, false);
            WeaponInputListener.w011GameObject.SetActive(false);
        }


    }
}