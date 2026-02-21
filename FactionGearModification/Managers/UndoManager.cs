using System;
using System.Collections.Generic;
using Verse;

namespace FactionGearCustomizer
{
    public static class UndoManager
    {
        private const int MaxUndoSteps = 30;
        private static LinkedList<KindGearData> undoList = new LinkedList<KindGearData>();
        private static Stack<KindGearData> redoStack = new Stack<KindGearData>();
        private static readonly object lockObj = new object();

        // Tracks the object currently being modified to ensure we don't mix up contexts
        private static string currentContextKindDefName = null;

        public static bool CanUndo
        {
            get
            {
                lock (lockObj)
                {
                    return undoList.Count > 0;
                }
            }
        }

        public static bool CanRedo
        {
            get
            {
                lock (lockObj)
                {
                    return redoStack.Count > 0;
                }
            }
        }

        public static void RecordState(KindGearData currentState)
        {
            if (currentState == null) return;

            lock (lockObj)
            {
                // If context changed (user switched to another pawn kind), clear stacks
                if (currentContextKindDefName != currentState.kindDefName)
                {
                    ClearInternal();
                    currentContextKindDefName = currentState.kindDefName;
                }

                // Create a snapshot
                KindGearData snapshot = currentState.DeepCopy();
                
                // Add to undo list
                undoList.AddLast(snapshot);
                
                // Limit stack size by removing oldest
                if (undoList.Count > MaxUndoSteps)
                {
                    undoList.RemoveFirst();
                }

                // New action clears redo history
                redoStack.Clear();
            }
        }

        public static void Undo(KindGearData targetState)
        {
            if (targetState == null) return;

            lock (lockObj)
            {
                if (undoList.Count == 0) return;

                // Check context
                if (currentContextKindDefName != targetState.kindDefName)
                {
                    ClearInternal();
                    return;
                }

                // Save current state to Redo stack before restoring
                redoStack.Push(targetState.DeepCopy());

                // Restore
                KindGearData previousState = undoList.Last.Value;
                undoList.RemoveLast();
                
                targetState.CopyFrom(previousState);
            }
        }

        public static void Redo(KindGearData targetState)
        {
            if (targetState == null) return;

            lock (lockObj)
            {
                if (redoStack.Count == 0) return;

                // Check context
                if (currentContextKindDefName != targetState.kindDefName)
                {
                    ClearInternal();
                    return;
                }

                // Save current state to Undo stack (as if it was recorded)
                undoList.AddLast(targetState.DeepCopy());
                if (undoList.Count > MaxUndoSteps) undoList.RemoveFirst();

                // Restore
                KindGearData nextState = redoStack.Pop();
                targetState.CopyFrom(nextState);
            }
        }

        public static void Clear()
        {
            lock (lockObj)
            {
                ClearInternal();
            }
        }

        private static void ClearInternal()
        {
            undoList.Clear();
            redoStack.Clear();
            currentContextKindDefName = null;
        }
    }
}
