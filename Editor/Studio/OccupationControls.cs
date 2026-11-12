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
    /// The few controls the Occupation Inspectors need that the kit does not have: an hour of the day shown as a clock time, a place-name
    /// field with a menu of the names the scene knows, a short text choice in a chosen order, and a text field bound to a string. All of
    /// them edit a <see cref="SerializedProperty"/> like the kit's own controls (Undo, prefab marks).
    /// </summary>
    internal static class OccupationControls
    {
        // Hours snap to five minutes.
        private const float HourStep = 1f / 12f;

        /// <summary>
        /// A row with a 0 to 24 slider and the hour written as a clock time (14.5 shows "14:30"); what is dragged snaps to five minutes.
        /// </summary>
        /// <param name="property">A float property holding an hour.</param>
        /// <param name="label">The row's label.</param>
        public static VisualElement HourRow(SerializedProperty property, string label)
        {
            property = property.Copy();
            var slider = new Slider(0f, 24f);
            slider.BindProperty(property);
            slider.tooltip = property.tooltip;
            slider.style.flexGrow = 1f;
            slider.style.flexShrink = 1f;
            slider.style.marginLeft = 0f;
            slider.style.marginRight = 0f;

            var clock = new Label(OccupationClock.Format(property.floatValue));
            clock.style.minWidth = 44f;
            clock.style.unityTextAlign = TextAnchor.MiddleRight;
            clock.style.unityFontStyleAndWeight = FontStyle.Bold;

            slider.RegisterValueChangedCallback(evt =>
            {
                float snapped = Mathf.Round(evt.newValue / HourStep) * HourStep;
                clock.text = OccupationClock.Format(snapped);
                if (Mathf.Abs(snapped - evt.newValue) > 0.0001f)
                {
                    property.floatValue = snapped;
                    property.serializedObject.ApplyModifiedProperties();
                }
            });

            VisualElement control = VeneerStyle.Row();
            control.Add(slider);
            control.Add(clock);
            VisualElement row = VeneerStyle.FieldRow(label, control, property.tooltip, out Label title);
            VeneerBinding.Hook(row, property, title, row, () => clock.text = OccupationClock.Format(property.floatValue));
            return row;
        }

        /// <summary>The layout entry that holds <paramref name="path"/> in the layout being drawn (its label, icon...), or null.</summary>
        /// <param name="context">The context a custom widget is built with.</param>
        /// <param name="path">The property path.</param>
        public static VeneerLayoutEntry EntryOf(VeneerCustomItemContext context, string path)
        {
            VeneerLayout layout = context.Layout;
            return layout != null ? layout.FindEntry(path, out _, out _) : null;
        }

        /// <summary>A labelled text field bound to a string property.</summary>
        /// <param name="property">A string property.</param>
        /// <param name="label">The row's label.</param>
        public static VisualElement TextRow(SerializedProperty property, string label)
        {
            property = property.Copy();
            var field = new TextField { tooltip = property.tooltip };
            field.BindProperty(property);
            field.style.flexGrow = 1f;
            field.style.marginLeft = 0f;
            field.style.marginRight = 0f;
            VisualElement row = VeneerStyle.FieldRow(label, field, property.tooltip, out Label title);
            VeneerBinding.Hook(row, property, title, row, null);
            return row;
        }

        /// <summary>
        /// A row for a place name: a text field and a button that lists the names the scene knows ("Home", every Occupation Place name) plus
        /// "none: stay where it stands".
        /// </summary>
        /// <param name="property">The string property of the place.</param>
        /// <param name="brand">Brand for the icon lookup.</param>
        public static VisualElement PlaceRow(SerializedProperty property, VeneerBrand brand)
        {
            property = property.Copy();
            SerializedObject owner = property.serializedObject;
            string path = property.propertyPath;

            var field = new TextField { tooltip = property.tooltip };
            field.BindProperty(property);
            field.style.flexGrow = 1f;
            field.style.flexShrink = 1f;
            field.style.marginLeft = 0f;
            field.style.marginRight = 0f;
            Button pick = VeneerStyle.IconButton("chevron-down", "Pick one of the place names the scene knows.", null, brand);
            pick.clicked += () => ShowPlaceMenu(owner, path);

            VisualElement control = VeneerStyle.Row();
            control.Add(field);
            control.Add(pick);
            VisualElement row = VeneerStyle.FieldRow("Place", control, property.tooltip, out Label title);
            VeneerBinding.Hook(row, property, title, row, null);
            return row;
        }

        /// <summary>
        /// A segmented choice for an enum whose buttons are shown in a chosen order (the saved numbers do not change): button i writes
        /// <c>values[i]</c> as the enum index.
        /// </summary>
        /// <param name="property">An enum property.</param>
        /// <param name="label">The row's label.</param>
        /// <param name="texts">Text of each button, in display order.</param>
        /// <param name="values">Enum index each button stands for.</param>
        /// <param name="brand">Brand for the icon lookup.</param>
        public static VisualElement ChoiceRow(SerializedProperty property, string label, string[] texts, int[] values, VeneerBrand brand)
        {
            property = property.Copy();
            VisualElement group = VeneerStyle.SegmentedIcons(new string[texts.Length], texts, -1, index =>
            {
                property.enumValueIndex = values[index];
                property.serializedObject.ApplyModifiedProperties();
            }, brand);
            group.style.flexGrow = 0f;
            VisualElement row = VeneerStyle.FieldRow(label, group, property.tooltip, out Label title);
            VeneerBinding.Hook(row, property, title, row, () =>
            {
                int selected = -1;
                if (!property.hasMultipleDifferentValues)
                {
                    selected = Array.IndexOf(values, property.enumValueIndex);
                }

                VeneerStyle.SetSegmentedSelected(group, selected);
            });
            return row;
        }

        /// <summary>
        /// Hides the card that holds <paramref name="element"/> outside Play Mode, for a card made only for Play Mode content ("Right now").
        /// The card stays visible while it holds anything else besides its title, so an element a user moved into a busy card does not hide
        /// that card. Does nothing when the element is not directly inside a card.
        /// </summary>
        /// <param name="builder">The builder that is drawing the Inspector (it evaluates the rule on its 200 ms poll).</param>
        /// <param name="element">The element that was just added to the card.</param>
        public static void HideCardOutsidePlay(VeneerInspectorBuilder builder, VisualElement element)
        {
            VisualElement card = element != null ? element.parent : null;
            if (card == null || !card.ClassListContains("vn-card"))
            {
                return;
            }

            builder.VisibleWhen(card, () => Application.isPlaying || HoldsOtherContent(card, element), true);
        }

        private static bool HoldsOtherContent(VisualElement card, VisualElement own)
        {
            for (int i = 0; i < card.childCount; i++)
            {
                VisualElement child = card[i];
                if (child != own && !child.ClassListContains("vn-card__title"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shows <paramref name="element"/> only while <paramref name="visible"/> returns true for the watched properties; re-evaluated when
        /// any of them changes. Safe after the element left its panel.
        /// </summary>
        /// <param name="element">The element to show or hide.</param>
        /// <param name="visible">The rule, read from the current values.</param>
        /// <param name="watched">The properties the rule depends on (they are copied).</param>
        public static void ShowWhen(VisualElement element, Func<bool> visible, params SerializedProperty[] watched)
        {
            VeneerStyle.SetDisplay(element, visible());
            for (int i = 0; i < watched.Length; i++)
            {
                element.TrackPropertyValue(watched[i].Copy(), changed =>
                {
                    if (element.panel != null)
                    {
                        VeneerStyle.SetDisplay(element, visible());
                    }
                });
            }
        }

        private static void ShowPlaceMenu(SerializedObject owner, string path)
        {
            var menu = new GenericMenu();
            SerializedProperty current = owner.FindProperty(path);
            string currentValue = current != null ? current.stringValue : string.Empty;
            var names = new List<string> { OccupationAgent.HomeName };
            foreach (OccupationPlace scenePlace in OccupationChecks.ScenePlaces())
            {
                if (scenePlace != null && !string.IsNullOrWhiteSpace(scenePlace.PlaceName) && !names.Contains(scenePlace.PlaceName))
                {
                    names.Add(scenePlace.PlaceName);
                }
            }

            foreach (string name in names)
            {
                string chosen = name;
                menu.AddItem(new GUIContent(chosen), currentValue == chosen, () => SetPlace(owner, path, chosen));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("(none: stay where it stands)"), string.IsNullOrEmpty(currentValue), () => SetPlace(owner, path, string.Empty));
            menu.ShowAsContext();
        }

        private static void SetPlace(SerializedObject owner, string path, string value)
        {
            owner.Update();
            owner.FindProperty(path).stringValue = value;
            owner.ApplyModifiedProperties();
        }
    }
}
