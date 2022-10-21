﻿using Verse;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using RimWorld;

namespace CrunchyDuck.Math {
	// TODO: unpause amount still goes up/down when target amount is changed with buttons. Low priority fix.
	class PatchTextFieldNumeric {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Widgets), "TextFieldNumeric", generics: new System.Type[] { typeof(int) });
		}

		public static bool Prefix(Rect rect, ref int val, ref string buffer, float min = 0.0f, float max = 1E+09f) {
			if (PatchDoWindowContents.bill_dialogue != null) {
				BillComponent bc = BillManager.AddGetBillComponent(PatchDoWindowContents.BillRef);
				bool is_repeat = bc.targetBill.repeatMode == BillRepeatModeDefOf.RepeatCount;
				bool is_target = bc.targetBill.repeatMode == BillRepeatModeDefOf.TargetCount && !PatchDoWindowContents.didTargetCount;
				bool is_unpause = bc.targetBill.repeatMode == BillRepeatModeDefOf.TargetCount && PatchDoWindowContents.didTargetCount;
				if (is_target)
					PatchDoWindowContents.didTargetCount = true;

				if (is_repeat)
					return PrefixExtended(rect, ref val, ref buffer, bc, ref bc.repeat_count_last_valid, is_unpause);
				// Rendering target count
				else if (is_target)
					return PrefixExtended(rect, ref val, ref buffer, bc, ref bc.target_count_last_valid, is_unpause);
				else if (is_unpause)
					return PrefixExtended(rect, ref val, ref buffer, bc, ref bc.unpause_last_valid, is_unpause);
			}
			return true;
		}

		/// This is its own function so that I can use ref string field to clean up my code. C# has stupid rules about local refs.
		public static bool PrefixExtended(Rect rect, ref int val, ref string buffer, BillComponent bc, ref string field, bool is_unpause) {
			// I don't think this happens.
			if (bc.targetBill.repeatMode == BillRepeatModeDefOf.Forever)
				return false;

			// Fill buffer for first time.
			if (buffer == null)
				buffer = field;
			// Rimworld has some fucky logic for the unpause field. This overrides that logic.
			else if (is_unpause)
				buffer = bc.unpause_buffer;

			// This checks if the user pressed the + or - buttons.
			string equation = is_unpause ? bc.unpause_last_valid : field;
			int test_val = val;
			Math.DoMath(equation, ref test_val, bc);
			// User pressed one of the buttons, and we should clear the field and accept the number.
			if (test_val != val) {
				field = val.ToString();
				if (is_unpause)
					bc.unpause_buffer = val.ToString();
				return false;
			}

			string name = "TextField" + rect.y.ToString("F0") + rect.x.ToString("F0");
			GUI.SetNextControlName(name);
			string str = Widgets.TextField(rect, buffer);
			if (buffer != str) {
				buffer = str;
				if (is_unpause)
					bc.unpause_buffer = buffer;

				// Try to evaluate equation
				if (Math.DoMath(buffer, ref val, bc)) {
					// Update last valid.
					field = buffer;
				}
			}
			return false;
		}
	}

}