using System;
using System.Collections.Generic;
using Veneer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace OccupationSystem
{
    /// <summary>
    /// The plans of an <see cref="OccupationData"/> in the studio look: one tab per plan (and a plus button for a new plan), then the
    /// selected plan's name and a card per step with buttons to move or delete it. It edits the same serialized fields as before, so the
    /// saved data does not change. The plan tabs and step cards are rebuilt only when the structure changes (a plan or step added, removed,
    /// moved, or the selected plan switched, Undo included); renaming a plan or a step only updates the texts. Nothing runs while nothing
    /// changes.
    /// </summary>
    internal sealed class OccupationPlansView : VisualElement
    {
        private const string TabKey = "OccupationSystem.PlanTab";

        private static readonly string[] GaitTexts = { "Idle", "Walk", "Run" };

        // Idle, Walk, Run in that order (the saved values keep Walk = 0 and Run = 1, so saved steps stay what they were).
        private static readonly int[] GaitValues = { (int)OccupationGait.Idle, (int)OccupationGait.Walk, (int)OccupationGait.Run };

        private static readonly string[] StayTexts = { "Nothing", "Minutes", "Until hour" };
        private static readonly int[] StayValues = { (int)OccupationStay.Nothing, (int)OccupationStay.Minutes, (int)OccupationStay.UntilHour };

        private readonly SerializedProperty plans;
        private readonly VeneerBrand brand;
        private readonly VisualElement tabHost;
        private readonly VisualElement content;
        private readonly List<string> shownPlanNames = new List<string>(8);
        private readonly List<string> shownStepNames = new List<string>(16);
        private readonly List<Label> stepTitles = new List<Label>(16);
        private int shownPlanCount = -1;
        private int shownSelected = -1;
        private int shownStepCount = -1;

        /// <summary>Builds the view for the <c>Plans</c> property of an occupation and keeps it in step with the data.</summary>
        /// <param name="plansProperty">The plans array property.</param>
        /// <param name="brand">Brand for the icon lookup.</param>
        public OccupationPlansView(SerializedProperty plansProperty, VeneerBrand brand)
        {
            plans = plansProperty.Copy();
            this.brand = brand;

            tabHost = VeneerStyle.Row();
            Add(tabHost);
            content = new VisualElement();
            Add(content);

            Rebuild();
            this.TrackSerializedObjectValue(plans.serializedObject, changed => Sync());
        }

        private int Selected
        {
            get
            {
                int count = plans.arraySize;
                return count == 0 ? 0 : Mathf.Clamp(SessionState.GetInt(TabKey, 0), 0, count - 1);
            }
        }

        // Called after any change of the data: rebuilds when the structure differs from what is shown, else only refreshes the texts.
        private void Sync()
        {
            if (panel == null || plans.serializedObject.targetObject == null)
            {
                return;
            }

            int count = plans.arraySize;
            int selected = Selected;
            int stepCount = count == 0 ? 0 : StepsOf(selected).arraySize;
            if (count != shownPlanCount || selected != shownSelected || stepCount != shownStepCount)
            {
                Rebuild();
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (!string.Equals(PlanLabel(i), shownPlanNames[i], StringComparison.Ordinal))
                {
                    BuildTabs();
                    break;
                }
            }

            if (count > 0)
            {
                SerializedProperty steps = StepsOf(selected);
                for (int i = 0; i < stepTitles.Count && i < steps.arraySize; i++)
                {
                    string name = steps.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(OccupationStep.Name)).stringValue;
                    if (!string.Equals(name, shownStepNames[i], StringComparison.Ordinal))
                    {
                        shownStepNames[i] = name;
                        stepTitles[i].text = (i + 1) + ". " + name;
                    }
                }
            }
        }

        private void Rebuild()
        {
            BuildTabs();
            BuildContent();
        }

        private SerializedProperty StepsOf(int planIndex) =>
            plans.GetArrayElementAtIndex(planIndex).FindPropertyRelative(nameof(OccupationPlan.Steps));

        private string PlanLabel(int index)
        {
            string name = plans.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(OccupationPlan.Name)).stringValue;
            return string.IsNullOrEmpty(name) ? "Plan " + (index + 1) : name;
        }

        // ------------------------------------------------------------------ plans

        private void BuildTabs()
        {
            tabHost.Clear();
            shownPlanNames.Clear();
            int count = plans.arraySize;
            shownPlanCount = count;
            if (count > 0)
            {
                var labels = new string[count];
                for (int i = 0; i < count; i++)
                {
                    labels[i] = PlanLabel(i);
                    shownPlanNames.Add(labels[i]);
                }

                VisualElement tabs = VeneerStyle.Tabs(null, labels, null, Selected, OnTabPicked, brand);
                tabs.style.flexGrow = 1f;
                tabs.style.flexWrap = Wrap.Wrap;
                tabHost.Add(tabs);
            }
            else
            {
                tabHost.Add(VeneerStyle.Spacer());
            }

            tabHost.Add(VeneerStyle.IconButton("plus", "Add a plan (one day).", AddPlan, brand));
        }

        private void OnTabPicked(int index)
        {
            SessionState.SetInt(TabKey, index);
            BuildContent();
            VeneerStyle.Reveal(content);
        }

        private void AddPlan()
        {
            SerializedObject owner = plans.serializedObject;
            owner.Update();
            plans.arraySize++;
            SerializedProperty plan = plans.GetArrayElementAtIndex(plans.arraySize - 1);
            plan.FindPropertyRelative(nameof(OccupationPlan.Name)).stringValue = "Plan " + plans.arraySize;
            plan.FindPropertyRelative(nameof(OccupationPlan.Steps)).arraySize = 0;
            owner.ApplyModifiedProperties();
            SessionState.SetInt(TabKey, plans.arraySize - 1);
            Rebuild();
        }

        private void RemovePlan(int index)
        {
            SerializedObject owner = plans.serializedObject;
            owner.Update();
            if (index < 0 || index >= plans.arraySize)
            {
                return;
            }

            plans.DeleteArrayElementAtIndex(index);
            owner.ApplyModifiedProperties();
            SessionState.SetInt(TabKey, Mathf.Max(index - 1, 0));
            Rebuild();
        }

        // ------------------------------------------------------------------ content of the selected plan

        private void BuildContent()
        {
            content.Clear();
            stepTitles.Clear();
            shownStepNames.Clear();
            int count = plans.arraySize;
            shownPlanCount = count;
            shownSelected = Selected;
            if (count == 0)
            {
                shownStepCount = 0;
                VisualElement empty = VeneerStyle.Card("No plan yet", "calendar", brand);
                empty.Add(VeneerStyle.Hint("A plan is one day: a list of steps. Add one, or several to vary the days.", "info", brand));
                empty.Add(VeneerStyle.Button("Add a plan", "plus", "Adds a plan with no steps.", AddPlan, true, brand));
                content.Add(empty);
                return;
            }

            int planIndex = shownSelected;
            SerializedProperty plan = plans.GetArrayElementAtIndex(planIndex);
            SerializedProperty steps = StepsOf(planIndex);
            shownStepCount = steps.arraySize;

            VisualElement planCard = VeneerStyle.Card("Plan", "list", brand);
            planCard.Add(VeneerStyle.Hint("One day, step by step. Steps run in order from the day start.", "info", brand));
            VisualElement nameRow = OccupationControls.TextRow(plan.FindPropertyRelative(nameof(OccupationPlan.Name)), "Name");
            nameRow.Add(VeneerStyle.IconButton("trash", "Remove this plan and its steps.", () => RemovePlan(planIndex), brand));
            planCard.Add(nameRow);
            content.Add(planCard);

            int stepCount = steps.arraySize;
            for (int i = 0; i < stepCount; i++)
            {
                content.Add(BuildStep(steps, i, stepCount));
            }

            VisualElement add = VeneerStyle.Row();
            add.Add(VeneerStyle.Button("Add a step", "plus", "Adds a step at the end of this plan.", () => AddStep(steps), false, brand));
            content.Add(add);
        }

        private void AddStep(SerializedProperty steps)
        {
            SerializedObject owner = steps.serializedObject;
            owner.Update();
            steps.arraySize++;
            SerializedProperty added = steps.GetArrayElementAtIndex(steps.arraySize - 1);
            added.FindPropertyRelative(nameof(OccupationStep.Name)).stringValue = "Step " + steps.arraySize;
            added.FindPropertyRelative(nameof(OccupationStep.Clip)).objectReferenceValue = null;
            owner.ApplyModifiedProperties();
            BuildContent();
        }

        private void MoveStep(SerializedProperty steps, int index, int by)
        {
            SerializedObject owner = steps.serializedObject;
            owner.Update();
            int target = index + by;
            if (index < 0 || target < 0 || index >= steps.arraySize || target >= steps.arraySize)
            {
                return;
            }

            steps.MoveArrayElement(index, target);
            owner.ApplyModifiedProperties();
            BuildContent();
        }

        private void DeleteStep(SerializedProperty steps, int index)
        {
            SerializedObject owner = steps.serializedObject;
            owner.Update();
            if (index < 0 || index >= steps.arraySize)
            {
                return;
            }

            steps.DeleteArrayElementAtIndex(index);
            owner.ApplyModifiedProperties();
            BuildContent();
        }

        // ------------------------------------------------------------------ one step

        private VisualElement BuildStep(SerializedProperty steps, int index, int count)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(index);
            VisualElement card = VeneerStyle.Card(null, null, brand);

            // Title row: number and name (kept in step with the Name field), a "Start" pill on the first step, move and delete buttons.
            string name = step.FindPropertyRelative(nameof(OccupationStep.Name)).stringValue;
            var title = new VisualElement();
            title.AddToClassList("vn-card__title");
            title.Add(VeneerStyle.IconElement(index == 0 ? "flag" : "step", null, brand));
            var titleLabel = new Label((index + 1) + ". " + name);
            titleLabel.AddToClassList("vn-card__title-label");
            titleLabel.AddToClassList("vn-ellipsis");
            title.Add(titleLabel);
            stepTitles.Add(titleLabel);
            shownStepNames.Add(name);
            if (index == 0)
            {
                Label start = VeneerStyle.Pill("Start", "info");
                start.tooltip = "The starting point: when the game starts the NPC is put at the place of this step (of the step the time of day says, when it starts late) instead of walking there.";
                title.Add(start);
            }

            title.Add(VeneerStyle.Spacer());
            Button up = VeneerStyle.IconButton("arrow-up", "Move this step up.", () => MoveStep(steps, index, -1), brand);
            up.SetEnabled(index > 0);
            Button down = VeneerStyle.IconButton("arrow-down", "Move this step down.", () => MoveStep(steps, index, 1), brand);
            down.SetEnabled(index < count - 1);
            title.Add(up);
            title.Add(down);
            title.Add(VeneerStyle.IconButton("trash", "Delete this step.", () => DeleteStep(steps, index), brand));
            card.Add(title);

            card.Add(OccupationControls.TextRow(step.FindPropertyRelative(nameof(OccupationStep.Name)), "Name"));
            card.Add(OccupationControls.PlaceRow(step.FindPropertyRelative(nameof(OccupationStep.Place)), brand));
            card.Add(OccupationControls.ChoiceRow(step.FindPropertyRelative(nameof(OccupationStep.Gait)), "Gait", GaitTexts, GaitValues, brand));

            // Set off at.
            SerializedProperty useStart = step.FindPropertyRelative(nameof(OccupationStep.UseStartAt));
            VisualElement startChips = VeneerStyle.Row(true);
            startChips.Add(VeneerControls.ToggleChip(useStart, "Wait for an hour", "clock", brand));
            card.Add(startChips);
            VisualElement startAt = OccupationControls.HourRow(step.FindPropertyRelative(nameof(OccupationStep.StartAt)), "Set off at");
            card.Add(startAt);
            OccupationControls.ShowWhen(startAt, () => useStart.boolValue, useStart);

            // How long it stays.
            SerializedProperty stay = step.FindPropertyRelative(nameof(OccupationStep.Stay));
            card.Add(OccupationControls.ChoiceRow(stay, "Stay", StayTexts, StayValues, brand));
            VisualElement minutes = VeneerControls.NumberRow(step.FindPropertyRelative(nameof(OccupationStep.Minutes)), "Game minutes", "min");
            card.Add(minutes);
            OccupationControls.ShowWhen(minutes, () => stay.enumValueIndex == (int)OccupationStay.Minutes, stay);
            VisualElement until = OccupationControls.HourRow(step.FindPropertyRelative(nameof(OccupationStep.UntilHour)), "Until");
            card.Add(until);
            OccupationControls.ShowWhen(until, () => stay.enumValueIndex == (int)OccupationStay.UntilHour, stay);

            // The animation, only for a step that stays.
            SerializedProperty clip = step.FindPropertyRelative(nameof(OccupationStep.Clip));
            VisualElement clipRow = VeneerControls.ObjectSlot(clip, "Animation", typeof(AnimationClip), brand);
            card.Add(clipRow);
            OccupationControls.ShowWhen(clipRow, () => stay.enumValueIndex != (int)OccupationStay.Nothing, stay);
            VisualElement loopRow = VeneerStyle.Row(true);
            loopRow.Add(VeneerControls.ToggleChip(step.FindPropertyRelative(nameof(OccupationStep.LoopClip)), "Loop the animation", "refresh", brand));
            card.Add(loopRow);
            OccupationControls.ShowWhen(loopRow, () => stay.enumValueIndex != (int)OccupationStay.Nothing && clip.objectReferenceValue != null, stay, clip);
            return card;
        }
    }
}
