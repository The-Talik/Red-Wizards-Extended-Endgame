using HarmonyLib;
using RW;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static RWUI.Logging;

namespace RWUI
{
	public class Perks
	{
		[HarmonyPatch(typeof(PerksPanel), "ShowCharPerks")]
		public static class PerksPanel_ShowCharPerks_RweeSections
		{
			static bool Prefix(PerksPanel __instance)
			{
				if (__instance == null)
					return false;

				__instance.inGame = true;

				var validate_method = AccessTools.Method(typeof(PerksPanel), "Validate");
				if (validate_method != null)
					validate_method.Invoke(__instance, null);

				if (PChar.Char == null || PChar.Char.perks == null)
					return false; // same behavior as vanilla (just do nothing)

				var panel = __instance.transform.Find("Panel");

				PanelReset(panel);

				//remove original layout group.

				/*				var panel_rt = panel.GetComponent<RectTransform>();
								if (panel_rt != null)
								{
									// Your panel has sizeDelta.y = -522 which cancels parent height -> 0 height.
									panel_rt.sizeDelta = new Vector2(panel_rt.sizeDelta.x, 0f);
									panel_rt.anchoredPosition = new Vector2(panel_rt.anchoredPosition.x, 0f);
								}*/


				if (panel == null)
					return false;

				var perk_go = __instance.perkGO;
				if (perk_go == null)
					return false;

				var title_text = GetTitleText(__instance);

				var acquired_index_by_id = new Dictionary<int, int>();
				for (int i = 0; i < PChar.Char.perks.Count; i++)
					acquired_index_by_id[PChar.Char.perks[i].perkID] = i;


				var template_grid = panel.GetComponent<GridLayoutGroup>();

				logr.Log($"panel children: {panel.childCount}");

				var sections_container = EnsurePanelIsSectionsContainer(panel);


				if (sections_container == null)
				{
					logr.Error("no section container");
					return false;
				}

				var bg_grid = GetOrCreateSectionGrid(sections_container, "RWEE_BG", "Background Perks", title_text, null);
				var xp_grid = GetOrCreateSectionGrid(sections_container, "RWEE_XP", "Experience", title_text, template_grid);
				var feat_grid = GetOrCreateSectionGrid(sections_container, "RWEE_FEAT", "Feat", title_text, template_grid);
				var karma_grid = GetOrCreateSectionGrid(sections_container, "RWEE_KARMA", "Karma", title_text, template_grid);

				FillSection(bg_grid, PerkType.Background, perk_go, acquired_index_by_id, 0.30f, false);
				FillSection(xp_grid, PerkType.Experience, perk_go, acquired_index_by_id, 0.30f, true); // hide missing
				FillSection(feat_grid, PerkType.Feat, perk_go, acquired_index_by_id, 0.30f, false);
				FillSection(karma_grid, PerkType.Karma, perk_go, acquired_index_by_id, 0.30f, false);

				/*				ForceRebuild(panel);
								var adjust = AccessTools.Method(typeof(PerksPanel), "AdjustPanelSize");
								if (adjust != null)
								{
									// Count ONLY perk icons actually shown (not headers)
									int shown = sections_container.GetComponentsInChildren<PerkControl>(true).Length;
									adjust.Invoke(__instance, new object[] { shown });
								}
								var scroll = __instance.GetComponentInChildren<ScrollRect>(true);
								if (scroll != null)
								{
									Canvas.ForceUpdateCanvases();
									scroll.verticalNormalizedPosition = 1f; // snap to top
									Canvas.ForceUpdateCanvases();
								}*/
				//DebugBg(panel, 1f, 0f, 0f, 0.15f);           // red: panel bounds
																										 //DebugBg(sections_container, 0f, 1f, 0f, 0.12f);   // green: sections root
				logr.Log($"panel children: {panel.childCount}");
				//DebugBg(bg_grid.parent, 1f, 0f, 1f, 0.10f);  // magenta: Background section container
				//DebugBg(bg_grid, 1f, 0.5f, 0f, 0.10f);       // orange: Background content grid
				return false;
			}
		}
		static void PanelReset(Transform panel)
		{
			if (panel == null)
				return;
			var grid_layout = panel.GetComponent<GridLayoutGroup>();
			if (grid_layout != null)
				UnityEngine.Object.DestroyImmediate(grid_layout);
		}
		static Text GetTitleText(PerksPanel instance)
		{
			var title_t = instance.transform.Find("Title");
			if (title_t == null)
				return null;

			return title_t.GetComponent<Text>();
		}
		static Transform EnsurePanelIsSectionsContainer(Transform panel)
		{
			if (panel == null)
				return null;

			var vertical_layout = panel.GetComponent<VerticalLayoutGroup>();
			if (vertical_layout == null)
			{
				vertical_layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
			}
			if(vertical_layout == null)
			{
				//This happens the first time the window opens.
				logr.Error("Could not add VerticalLayoutGroup to perks panel");
				return null;
			}

			
			vertical_layout.childControlWidth = true;
			vertical_layout.childForceExpandWidth = true;
			vertical_layout.childControlHeight = true;
			vertical_layout.childForceExpandHeight = false;
			vertical_layout.childScaleHeight = false;
			vertical_layout.spacing = 6f;
/*
			for (int i = panel.childCount - 1; i >= 0; i--)
				UnityEngine.Object.Destroy(panel.GetChild(i).gameObject);*/

			return panel;
		}

		static Transform GetOrCreateSectionGrid(Transform parent, string section_name, string header_text, Text title_template, GridLayoutGroup template_grid)
		{
			if (parent == null)
				return null;

			var section = parent.Find(section_name);
			if (section == null)
			{
				logr.Log($"Creating section {section_name}");
				var section_go = new GameObject(section_name, typeof(RectTransform));
				
				section_go.transform.SetParent(parent, false);
				section = section_go.transform;
				var section_le = section_go.GetComponent<LayoutElement>() ?? section_go.AddComponent<LayoutElement>();
				section_le.flexibleHeight = 0f;
			
				var section_vlg = section_go.AddComponent<VerticalLayoutGroup>();
				section_vlg.childControlWidth = true;
				section_vlg.childForceExpandWidth = true;
				section_vlg.childControlHeight = true;
				section_vlg.childForceExpandHeight = false;
				section_vlg.childScaleHeight = false;
				section_vlg.spacing = 0f;
				var header_go = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
				header_go.transform.SetParent(section, false);
				var header = header_go.GetComponent<Text>();
				header.text = header_text;
				if (title_template != null)
				{
					header.font = title_template.font;
					header.fontSize = title_template.fontSize > 2 ? (title_template.fontSize - 2) : title_template.fontSize;
					header.color = title_template.color;
				}

				var header_le = header_go.AddComponent<LayoutElement>();
				header_le.preferredHeight = 16f;
				header_le.minHeight = 16f;
				header_le.flexibleHeight = 0f;

				var content_go = new GameObject("Content", typeof(RectTransform));
				content_go.transform.SetParent(section, false);
				var content_grid = content_go.AddComponent<GridLayoutGroup>();
				ApplyGridTemplate(content_grid, template_grid);

				var content_fitter = content_go.AddComponent<ContentSizeFitter>();
				content_fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
				content_fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			}

			section.gameObject.SetActive(true);

			var content = section.Find("Content");
			if (content != null)
				content.gameObject.SetActive(true);

			return content;
		}

		static void ApplyGridTemplate(GridLayoutGroup dst, GridLayoutGroup src)
		{
			if (dst == null)
				return;

			if (src != null)
			{
				dst.cellSize = src.cellSize;
				dst.spacing = src.spacing;
				dst.padding = src.padding;
				dst.constraint = src.constraint;
				dst.constraintCount = src.constraintCount;

				// FORCE top-left, regardless of prefab settings
				dst.childAlignment = TextAnchor.UpperLeft;
				dst.startCorner = GridLayoutGroup.Corner.UpperLeft;
				dst.startAxis = GridLayoutGroup.Axis.Horizontal;

				return;
			}

			dst.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			dst.constraintCount = 14;
			dst.cellSize = new Vector2(64f, 64f);
			dst.spacing = new Vector2(6f, 6f);
			dst.childAlignment = TextAnchor.UpperLeft;
			dst.startCorner = GridLayoutGroup.Corner.UpperLeft;
			dst.startAxis = GridLayoutGroup.Axis.Horizontal;

		}

		static void FillSection(Transform content, PerkType perk_type, GameObject perk_go, Dictionary<int, int> acquired_index_by_id, float missing_alpha, bool hide_missing)
		{
			if (content == null || perk_go == null)
				return;

			int total_perks = PerkDB.totalPerks;
			int i = 0;

			for (int pass = 0; pass < 2; pass++)
			{
				for (int j = 0; j < total_perks; j++)
				{
					var perk = PerkDB.GetByIndex(j);
					if (perk == null)
						continue;

					if (perk.type != perk_type)
						continue;

					if (!GameData.HasExpansion(perk.expansion))
						continue;

					bool is_acquired = acquired_index_by_id.ContainsKey(perk.id);

					if (hide_missing && !is_acquired)
						continue;

					if (pass == 0 && !is_acquired)
						continue;

					if (pass == 1 && is_acquired)
						continue;

					if (i >= content.childCount)
						UnityEngine.Object.Instantiate(perk_go, content);

					var item = content.GetChild(i);
					var perk_control = item.GetComponent<PerkControl>();

					if (is_acquired)
					{
						int perk_index = acquired_index_by_id[perk.id];
						perk_control.Setup(perk, null, null, true, PChar.Char.perks[perk_index]);
						SetUiAlpha(item.gameObject, 1f);
					}
					else
					{
						perk_control.Setup(perk, null, null, false, null);
						SetUiAlpha(item.gameObject, missing_alpha);
					}

					item.gameObject.SetActive(true);
					i++;
				}
			}
			FixGridHeight(content, i);
			while (i < content.childCount)
			{
				content.GetChild(i).gameObject.SetActive(false);
				i++;
			}

		}

		static void SetUiAlpha(GameObject root, float alpha)
		{
			if (root == null)
				return;

			var graphics = root.GetComponentsInChildren<Graphic>(true);
			for (int i = 0; i < graphics.Length; i++)
			{
				var g = graphics[i];
				if (g == null)
					continue;

				var c = g.color;
				c.a = alpha;
				g.color = c;
			}
		}

		static void ForceRebuild(Transform panel)
		{
			if (panel == null)
				return;

			var rt = panel.GetComponent<RectTransform>();
			if (rt == null)
				return;

			LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
		}
		static void FixGridHeight(Transform content, int item_count)
		{
			var grid = content.GetComponent<GridLayoutGroup>();
			if (grid == null)
				return;

			int cols = 1;
			if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
				cols = Mathf.Max(1, grid.constraintCount);

			int rows = (item_count + cols - 1) / cols;

			float height = grid.padding.top + grid.padding.bottom;
			if (rows > 0)
				height += (rows * grid.cellSize.y) + ((rows - 1) * grid.spacing.y);

			var le = content.GetComponent<LayoutElement>();
			if (le == null)
				le = content.gameObject.AddComponent<LayoutElement>();

			le.minHeight = height;
			le.preferredHeight = height;
			le.flexibleHeight = 0f;

			grid.childAlignment = TextAnchor.UpperLeft;
		}
		static void DebugBg(Transform target, float r, float g, float b, float a)
		{
			if (target == null)
				return;

			var img = target.GetComponent<Image>();
			if (img == null)
				img = target.gameObject.AddComponent<Image>();

			img.color = new Color(r, g, b, a);
			img.raycastTarget = false;
		}
		/**
		 * show perk unlock even if already unlocked
		 */
		[HarmonyPatch(typeof(Perk), "GetString")]
		public static class Perk_GetString
		{
			static void Postfix(bool justUnlocked, ref bool ___locked, ref bool ___showLockState, Perk __instance, int ___showLevel, ref string __result)
			{
				if ((___showLockState && ___locked) || justUnlocked)
					return;
				if (__instance.UnlockText == "")
					return;
				__result += "\n";

				if(__instance.type == PerkType.Background)
					__result += "\n";
				
				if (!justUnlocked)
				{
					__result += "<size=12>To Unlock:</size>\n";
				}
				__result += ColorSys.infoText2 + __instance.UnlockText + "</color>";

				if (__instance.type != PerkType.Background)
					__result += "\n";
			}
		}
	}
}
