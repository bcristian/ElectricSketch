using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Undo
{
    /// <summary>
    /// The manager of the undo system. Use it to record actions that change the system, and to undo/redo those actions.
    /// Derive your actions from <see cref="UndoableAction"/>.
    /// </summary>
    public sealed class UndoManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        readonly UndoableAction root = new UndoableAction();

        public UndoManager(string initialStateDescription)
        {
            root.CreateChildren();
            Stack.Push(root);
            root.PropertyChanged += OnRootPropertyChanged;
            Do(new UndoableAction() { Description = initialStateDescription });
        }

        void OnRootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UndoableAction.CanUndoChild))
                RaisePropertyChanged(nameof(CanUndo));
            if (e.PropertyName == nameof(UndoableAction.CanRedoChild))
                RaisePropertyChanged(nameof(CanRedo));
            if (e.PropertyName == nameof(UndoableAction.LatestChildIndex))
                RaisePropertyChanged(nameof(LatestActionIndex));
        }

        /// <summary>
        /// Past and future actions.
        /// </summary>
        public ReadOnlyObservableCollection<UndoableAction> History => root.Children;

        /// <summary>
        /// Index of the latest performed action. -1 if no prior actions.
        /// </summary>
        public int LatestActionIndex => root.LatestChildIndex;

        /// <summary>
        /// The currently running actions.
        /// </summary>
        public Stack<UndoableAction> Stack { get; private set; } = new Stack<UndoableAction>(); // Should be read-only

        /// <summary>
        /// What's currently happening (nothing/doing/undoing/redoing).
        /// </summary>
        public UndoManagerState State { get; private set; }

        /// <summary>
        /// The system will not attempt to merge actions more than this time apart.
        /// Set to zero to never attempt to merge. Set to <see cref="TimeSpan.MaxValue"/> to always attempt to merge.
        /// <seealso cref="UndoableAction.Merge(UndoableAction)"/>
        /// </summary>
        public TimeSpan MergeTimeLimit
        {
            get => mergeTimeLimit;
            set
            {
                if (mergeTimeLimit == value)
                    return;
                mergeTimeLimit = value;
                RaisePropertyChanged();
            }
        }
        TimeSpan mergeTimeLimit = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// If set, the next action will not be merged and the flag will be cleared.
        /// </summary>
        public bool DontMergeNextChange { get; set; }

        /// <summary>
        /// To change the state of the system create an action that does that and pass it to this method.
        /// The action will be recorded and executed.
        /// </summary>
        /// <param name="leaveOnStack">Leave the action on the stack, so that future actions will become its children.</param>
        /// <remarks>
        /// To create action groups, push an <see cref="UndoableAction"/>, leaving it on the stack, do the changes, then call <see cref="Pop(UndoableAction)"/> to finish the group.
        /// </remarks>
        public void Do(UndoableAction action, bool leaveOnStack = false)
        {
            if (State != UndoManagerState.Doing && State != UndoManagerState.Idle)
                throw new InvalidOperationException($"Cannot do actions while state is {State}");
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var parent = Stack.Peek();
            parent.CreateChildren();

            // Discard the future :)
            while (parent.CanRedoChild)
                parent.RemoveLastChild();

            try
            {
                State = UndoManagerState.Doing;
                Push(action);

                if (parent.Children.Count > 0 && MergeTimeLimit != TimeSpan.Zero && !DontMergeNextChange)
                    if (MergeTimeLimit == TimeSpan.MaxValue || action.Time - parent.Children[^1].Time <= MergeTimeLimit)
                        if (parent.Children[^1].Merge(action))
                        {
                            if (action.Children != null)
                                foreach (var mergedChild in action.Children)
                                    parent.Children[^1].AddChild(mergedChild);
                            return;
                        }

                parent.AddChild(action);

                DontMergeNextChange = false;

                action.Do();
            }
            finally
            {
                if (!leaveOnStack)
                    Pop(action);
            }
        }

        /// <summary>
        /// Removes any future actions.
        /// </summary>
        public void PurgeFuture()
        {
            if (State != UndoManagerState.Doing && State != UndoManagerState.Idle)
                throw new InvalidOperationException($"Cannot purge actions while state is {State}");
            var parent = Stack.Peek();
            while (parent.CanRedoChild)
                parent.RemoveLastChild();
        }

        void Push(UndoableAction action)
        {
            Stack.Push(action);
            if (Stack.Count == 2)
            {
                RaisePropertyChanged(nameof(CanUndo));
                RaisePropertyChanged(nameof(CanRedo));
            }
        }

        /// <summary>
        /// Removes the latest action from the stack. The parameter is the expected action, to safeguard against stack corruption.
        /// </summary>
        /// <param name="expected"></param>
        public void Pop(UndoableAction expected)
        {
            if (Stack.Pop() != expected)
                throw new InvalidOperationException("Undo stack corruption");
            if (Stack.Count == 1)
            {
                State = UndoManagerState.Idle;
                RaisePropertyChanged(nameof(CanUndo));
                RaisePropertyChanged(nameof(CanRedo));
            }
        }

        /// <summary>
        /// Creates an object that groups all subsequent operations into a single visible change, until it is disposed.
        /// </summary>
        /// <param name="description">the <see cref="UndoableAction.Description"/> of the change</param>
        public IDisposable CreateBatch(string description)
        {
            Do(new UndoableAction() { Description = description }, true);
            return new UndoBatchScope(this);
        }

        class UndoBatchScope : IDisposable
        {
            public UndoBatchScope(UndoManager manager)
            {
                this.manager = manager ?? throw new ArgumentNullException();
                batch = manager.Stack.Peek();
            }

            readonly UndoManager manager;
            readonly UndoableAction batch;

            public void Dispose()
            {
                manager.Pop(batch);
            }
        }

        /// <summary>
        /// Is there any action to undo?
        /// </summary>
        public bool CanUndo => Stack.Count == 1 && root.LatestChildIndex > 0; // because the initial state cannot be undone

        /// <summary>
        /// Undoes one action.
        /// </summary>
        public void Undo()
        {
            if (State != UndoManagerState.Undoing && State != UndoManagerState.Idle)
                throw new InvalidOperationException($"Cannot undo actions while state is {State}");
            if (!CanUndo)
                return;

            try
            {
                State = UndoManagerState.Undoing;
                root.UndoChild();
            }
            finally
            {
                State = UndoManagerState.Idle;
            }
        }

        /// <summary>
        /// Puts the system in the state it was after the specified action.
        /// </summary>
        public void GoToStateAfterAction(UndoableAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            var index = root.Children.IndexOf(action);
            if (index < 0)
                throw new ArgumentException(nameof(action));

            while (index < root.LatestChildIndex)
                Undo();
            while (index > root.LatestChildIndex)
                Redo();
        }

        /// <summary>
        /// Is there any action to redo?
        /// </summary>
        public bool CanRedo => Stack.Count == 1 && root.CanRedoChild;

        /// <summary>
        /// Redoes one action.
        /// </summary>
        public void Redo()
        {
            if (State != UndoManagerState.Redoing && State != UndoManagerState.Idle)
                throw new InvalidOperationException($"Cannot redo actions while state is {State}");
            if (!CanRedo)
                return;

            try
            {
                State = UndoManagerState.Redoing;
                root.RedoChild();
            }
            finally
            {
                State = UndoManagerState.Idle;
            }
        }
    }

    /// <summary>
    /// What is the manager currently doing.
    /// </summary>
    public enum UndoManagerState
    {
        /// <summary>
        /// No action running.
        /// </summary>
        Idle,

        /// <summary>
        /// Doing an action.
        /// </summary>
        Doing,

        /// <summary>
        /// Undoing an action.
        /// </summary>
        Undoing,

        /// <summary>
        /// Re-doing an action.
        /// </summary>
        Redoing
    }
}
