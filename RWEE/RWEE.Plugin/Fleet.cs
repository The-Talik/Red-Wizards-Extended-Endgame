using HarmonyLib;
using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RWEE
{
    internal class Fleet
    {
        /**
         * give less experience the higher above CL50 you are.
         */
        [HarmonyPatch(typeof(AIControl), "SearchForAsteroids")]
        static class AIControl_SearchForAsteroids
        {
            static void Prefix(AIControl __instance, ref float radius, ref SpaceShip ___ss)
            {
                radius = ___ss.stats.scannerPower;
                //___ss.GetShipModel();
                //Main.log($"Searcing for Asteroid: {radius} {___ss.stats.scannerPower}");

            }
        }
    }
}
