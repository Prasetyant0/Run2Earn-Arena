using UnityEngine;

namespace GameBalap.Core
{
    public static class UnityHelper
    {
        /// <summary>
        /// Mencari instance objek dengan kompatibilitas penuh untuk Unity 2020, 2021, 2022, 2023, hingga Unity 6.
        /// </summary>
        public static T FindInstance<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }
    }
}
