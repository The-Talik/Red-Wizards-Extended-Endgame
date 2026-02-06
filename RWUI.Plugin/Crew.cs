using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RW;
using static RWUI.Logging;
namespace RWUI
{
	internal class Crew
	{

			[HarmonyPatch(typeof(CrewMember), "GetNameModified", new Type[] { typeof(int), typeof(bool), typeof(bool) })]
			static class CrewMember_GetNameModified
			{
				static void Postfix(AICharacter ___aiChar, List<CrewSkill> ___skills, ref string __result)
				{
					if (___aiChar == null)
						return;
					__result += $" ({___aiChar.level})";
					if (___skills == null || ___skills.Count == 0)
						return;

					var abbrev_list = new List<string>(___skills.Count);

					for (int i = 0; i < ___skills.Count; i++)
					{
						var skill = ___skills[i];
						if (skill == null)
							continue;

						var skill_name = Lang.Get(23, 10 + ((int)skill.ID * (int)CrewPosition.Navigator));
						if (string.IsNullOrEmpty(skill_name))
							continue;

						skill_name = skill_name.Trim();
						var len = skill_name.Length < 3 ? skill_name.Length : 3;

						abbrev_list.Add(skill_name.Substring(0, len));
					}

					if (abbrev_list.Count == 0)
						return;

					__result = (__result ?? "") + " [" + string.Join(", ", abbrev_list) + "]";
				}
			
		}
	}
}