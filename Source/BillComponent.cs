﻿using RimWorld;
using Verse;
using System.Text.RegularExpressions;

namespace CrunchyDuck.Math {
	class BillComponent {
		public Bill_Production targetBill;
		private Regex oldCategory = new Regex(@"((?:c|cat|category) )(.+?)", RegexOptions.Compiled);
		public CachedMapData Cache {
			get {
				return Math.GetCachedMap(targetBill);
			}
		}
		public string name;

		// I have to maintain my own buffers so I can modify them at will, e.g. when a + or - button is pressed.
		public InputField itemsToCount;
		public InputField doXTimes;
		public InputField doUntilX;
		public InputField unpause;
		public bool customItemsToCount = false;
		public bool isDoXTimes { get { return targetBill.repeatMode == BillRepeatModeDefOf.RepeatCount; } }
		public bool isDoUntilX { get { return targetBill.repeatMode == BillRepeatModeDefOf.TargetCount; } }

		public BillComponent(Bill_Production bill) {
			targetBill = bill;
			name = bill.Label.CapitalizeFirst();
			doXTimes = new InputField(bill, InputField.Field.DoXTimes, this, 1);
			doUntilX = new InputField(bill, InputField.Field.DoUntilX, this);
			unpause = new InputField(bill, InputField.Field.Unpause, this, 5);

			itemsToCount = new InputField(bill, InputField.Field.itemsToCount, this);
			if (bill.recipe.ProducedThingDef != null) {
				itemsToCount.SetAll("\"" + bill.recipe.ProducedThingDef.label.ToParameter() + "\"");
			}
			else if (bill.recipe != null) {
				var spts = bill.recipe.specialProducts;
				if (spts != null) {
					// Check if butchery
					foreach (var spt in bill.recipe.specialProducts) {
						if (spt == SpecialProductType.Butchery) {
							itemsToCount.SetAll("\"category meat\"");
							break;
						}
					}
				}
			}
		}

		public void ExposeData() {
			// I wanna change these but that'd break peoples' saves lmao
			Scribe_Values.Look(ref name, "billName", targetBill.Label.CapitalizeFirst());
			Scribe_Values.Look(ref doXTimes.lastValid, "repeat_count_last_valid");
			doXTimes.buffer = doXTimes.lastValid;
			Scribe_Values.Look(ref doUntilX.lastValid, "target_count_last_valid");
			doUntilX.buffer = doUntilX.lastValid;
			Scribe_Values.Look(ref unpause.lastValid, "unpause_last_valid");
			unpause.buffer = unpause.lastValid;
			Scribe_Values.Look(ref itemsToCount.lastValid, "itemsToCountLastValid");
			itemsToCount.buffer = itemsToCount.lastValid;
			Scribe_Values.Look(ref this.customItemsToCount, "itemsToCountBool");

			// Back compatibility.
			itemsToCount.buffer = oldCategory.Replace(itemsToCount.buffer, m => "categories." + m.Groups[2]);
			doXTimes.buffer = oldCategory.Replace(doXTimes.buffer, m => "categories." + m.Groups[2]);
			doUntilX.buffer = oldCategory.Replace(doUntilX.buffer, m => "categories." + m.Groups[2]);
			unpause.buffer = oldCategory.Replace(unpause.buffer, m => "categories." + m.Groups[2]);

			Scribe_Values.Look(ref targetBill.targetCount, "target_count_last_result");
			Scribe_Values.Look(ref targetBill.repeatCount, "doXTimesLastResult");
			Scribe_Values.Look(ref targetBill.unpauseWhenYouHave, "unpauseLastResult");
		}
	}

	class InputField {
		private Bill_Production bill;
		public BillComponent bc;
		public Field field;
		public string lastValid = "";
		public string buffer = "";
		public int CurrentValue {
			get {
				switch (field) {
					case Field.DoUntilX:
						return bill.targetCount;
					case Field.DoXTimes:
						return bill.repeatCount;
					case Field.Unpause:
						return bill.unpauseWhenYouHave;
					default:
						return 0;
				}
			}
			set {
				switch (field) {
					case Field.DoUntilX:
						bill.targetCount = value;
						break;
					case Field.DoXTimes:
						bill.repeatCount = value;
						break;
					case Field.Unpause:
						bill.unpauseWhenYouHave = value;
						break;
					default:
						// itemsToCount works differently, see CountProducts_Patch
						break;
				}
			}
		}

		public InputField(Bill_Production bp, Field field, BillComponent bc, int default_value = 10) {
			bill = bp;
			this.field = field;
			this.bc = bc;
			SetAll(default_value);
		}

		public void SetAll(int value) {
			buffer = value.ToString();
			lastValid = value.ToString();
			CurrentValue = value;
		}

		public void SetAll(string str, int value) {
			buffer = str;
			lastValid = str;
			CurrentValue = value;
		}

		public void SetAll(string str) {
			buffer = str;
			lastValid = str;
			//int i = 0;
			//Math.DoMath(str, ref i, this);
			//CurrentValue = i;
		}

		public enum Field {
			itemsToCount,
			DoUntilX,
			DoXTimes,
			Unpause,
		}
	}
}
