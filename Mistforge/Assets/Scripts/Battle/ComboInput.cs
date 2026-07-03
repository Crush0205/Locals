using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mistforge
{
    /// The combo-queue UI (§6.2): up to 5 directional inputs via on-screen
    /// buttons or keyboard, a combo-trail display, and an Arts reference panel
    /// that live-highlights any Art the current queue is a valid prefix of.
    public class ComboInput : MonoBehaviour
    {
        public const int MaxInputs = 5;

        public Action<List<ComboDirection>> onConfirm;
        public Action onFocus;

        readonly List<ComboDirection> queue = new List<ComboDirection>();
        bool active;
        BattleActor actor;

        // UI references
        RectTransform root;
        TextMeshProUGUI trailText;
        TextMeshProUGUI confirmLabel;
        readonly Dictionary<ComboDirection, TextMeshProUGUI> directionLabels = new Dictionary<ComboDirection, TextMeshProUGUI>();

        class ArtRow
        {
            public ArtDef art;
            public TextMeshProUGUI text;
        }
        readonly List<ArtRow> artRows = new List<ArtRow>();
        RectTransform artsPanel;

        static readonly Color Idle = new Color(0.65f, 0.65f, 0.7f);
        static readonly Color Prefix = new Color(1f, 0.9f, 0.4f);
        static readonly Color Ready = new Color(0.45f, 1f, 0.5f);
        static readonly Color NoAP = new Color(1f, 0.45f, 0.4f);

        public void BuildUI(Transform canvasRoot)
        {
            // --- Input cluster: center-bottom ---
            root = UIFactory.Panel(canvasRoot, "ComboInput", new Color(0.08f, 0.09f, 0.12f, 0.85f));
            UIFactory.Place(root, new Vector2(0.26f, 0f), new Vector2(0.72f, 0f),
                new Vector2(0f, 8f), new Vector2(0f, 168f));

            trailText = UIFactory.Text(root, "", 26, TextAlignmentOptions.Center);
            UIFactory.Place(trailText.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);

            // Direction buttons with per-character attack names.
            var dirs = new[] { ComboDirection.Left, ComboDirection.Up, ComboDirection.Down, ComboDirection.Right };
            for (int i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                var button = UIFactory.Button(root, "", () => Push(dir), new Color(0.2f, 0.22f, 0.3f), 17f);
                var rect = (RectTransform)button.transform;
                float x0 = 0.02f + i * 0.15f;
                UIFactory.Place(rect, new Vector2(x0, 0.08f), new Vector2(x0 + 0.14f, 0.58f),
                    Vector2.zero, Vector2.zero);
                directionLabels[dir] = button.GetComponentInChildren<TextMeshProUGUI>();
            }

            var confirm = UIFactory.Button(root, "Attack", Confirm, new Color(0.55f, 0.35f, 0.15f), 19f);
            UIFactory.Place((RectTransform)confirm.transform, new Vector2(0.64f, 0.08f), new Vector2(0.86f, 0.58f),
                Vector2.zero, Vector2.zero);
            confirmLabel = confirm.GetComponentInChildren<TextMeshProUGUI>();

            var focus = UIFactory.Button(root, "Focus (+AP)", Focus, new Color(0.2f, 0.35f, 0.5f), 15f);
            UIFactory.Place((RectTransform)focus.transform, new Vector2(0.875f, 0.33f), new Vector2(0.99f, 0.58f),
                Vector2.zero, Vector2.zero);

            var clear = UIFactory.Button(root, "Clear", ClearQueue, new Color(0.3f, 0.25f, 0.28f), 15f);
            UIFactory.Place((RectTransform)clear.transform, new Vector2(0.875f, 0.08f), new Vector2(0.99f, 0.31f),
                Vector2.zero, Vector2.zero);

            // --- Arts reference panel: right edge ---
            artsPanel = UIFactory.Panel(canvasRoot, "ArtsPanel", new Color(0.08f, 0.09f, 0.12f, 0.8f));
            UIFactory.Place(artsPanel, new Vector2(0.73f, 0f), new Vector2(1f, 0f),
                new Vector2(4f, 8f), new Vector2(-4f, 226f));

            root.gameObject.SetActive(false);
            artsPanel.gameObject.SetActive(false);
        }

        public void Begin(BattleActor activeActor)
        {
            actor = activeActor;
            queue.Clear();
            active = true;
            root.gameObject.SetActive(true);
            artsPanel.gameObject.SetActive(true);

            foreach (var pair in directionLabels)
                pair.Value.text = pair.Key.ToString().Substring(0, 1) + "\n<size=70%>" +
                    actor.character.AttackName(pair.Key) + "</size>";

            RebuildArtRows();
            Refresh();
        }

        public void End()
        {
            active = false;
            if (root != null) root.gameObject.SetActive(false);
            if (artsPanel != null) artsPanel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!active) return;
            if (InputHelper.DirectionPressed(out var dir)) Push(dir);
            else if (InputHelper.ConfirmPressed()) Confirm();
            else if (InputHelper.FocusPressed()) Focus();
            else if (InputHelper.ClearPressed()) ClearQueue();
        }

        void Push(ComboDirection dir)
        {
            if (!active || queue.Count >= MaxInputs) return;
            queue.Add(dir);
            Refresh();
        }

        void ClearQueue()
        {
            if (!active) return;
            queue.Clear();
            Refresh();
        }

        void Confirm()
        {
            if (!active || queue.Count == 0) return;
            var sequence = new List<ComboDirection>(queue);
            onConfirm?.Invoke(sequence);
        }

        void Focus()
        {
            if (!active) return;
            onFocus?.Invoke();
        }

        void RebuildArtRows()
        {
            foreach (var row in artRows)
                if (row.text != null) Destroy(row.text.gameObject);
            artRows.Clear();

            var header = UIFactory.Text(artsPanel, actor.displayName + " — Arts", 17,
                TextAlignmentOptions.Left, new Color(0.9f, 0.75f, 0.4f));
            UIFactory.Place(header.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero);
            artRows.Add(new ArtRow { art = null, text = header });

            var arts = actor.character.arts;
            for (int i = 0; i < arts.Length; i++)
            {
                var text = UIFactory.Text(artsPanel, "", 15, TextAlignmentOptions.Left, Idle);
                float top = 0.86f - i * 0.14f;
                UIFactory.Place(text.rectTransform, new Vector2(0.04f, top - 0.13f), new Vector2(0.98f, top),
                    Vector2.zero, Vector2.zero);
                artRows.Add(new ArtRow { art = arts[i], text = text });
            }
        }

        /// Combo trail + live Art highlighting (§6.2).
        void Refresh()
        {
            var slots = new string[MaxInputs];
            for (int i = 0; i < MaxInputs; i++)
                slots[i] = i < queue.Count ? queue[i].ToString().Substring(0, 1) : "_";
            trailText.text = "Combo:  " + string.Join(" ", slots);

            var matched = ArtMatcher.Match(actor.character.arts, queue);
            bool affordable = matched != null && actor.ap >= matched.apCost;
            confirmLabel.text = matched == null ? "Attack"
                : affordable ? matched.displayName + "!"
                : matched.displayName + "\n<size=70%>(need " + matched.apCost + " AP)</size>";

            foreach (var row in artRows)
            {
                if (row.art == null) continue;
                string label = row.art.SequenceLabel() + "   " + row.art.displayName +
                    "  <size=80%>x" + row.art.damageMultiplier + ", " + row.art.apCost + " AP</size>";
                row.text.text = label;

                if (row.art == matched)
                    row.text.color = affordable ? Ready : NoAP;
                else if (queue.Count > 0 && ArtMatcher.IsPrefix(row.art, queue))
                    row.text.color = Prefix;
                else
                    row.text.color = Idle;
            }
        }
    }
}
