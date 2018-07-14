using System.Collections.Generic;

public class HexCellPriorityQueue {

    List<HexCell> list = new List<HexCell>();

    int count = 0;

    int minimum = int.MaxValue;

    public int Count {
        get {
            return count;
        }
    }

    public void Enqueue(HexCell cell) {
        count += 1;
        int priority = cell.SearchPriority;
        if (priority < minimum) {
            minimum = priority;
        }
        while (priority >= list.Count) {
            list.Add(null);
        }
        cell.NextWithSamePriority = list[priority]; // linked list
        list[priority] = cell;
    }

    public HexCell Dequeue () {
        count -= 1;
        for (; minimum < list.Count; minimum++) {
            HexCell cell = list[minimum];
            if (cell != null) {
                list[minimum] = cell.NextWithSamePriority;
                return cell;
            }
        }

        return null;
    }

    public void Change (HexCell cell, int oldPriority) {
        HexCell current = list[oldPriority];
        HexCell next = current.NextWithSamePriority;
        // if current is changed, it's the head of the list; cut it away
        if (current == cell) {
            list[oldPriority] = next;
        }
        // else follow chain to the cell in front of the changed cell
        else {
            while (next != cell) {
                current = next;
                next    = current.NextWithSamePriority;
            }
            // remove changed cell from linked list by skipping it
            current.NextWithSamePriority = cell.NextWithSamePriority;
        }
        // add it back so it ends up in the list for its new priority
        Enqueue(cell);
        count -= 1;     // we didn't add a cell, so compensate
    }

    public void Clear () {
        list.Clear();
        count = 0;
        minimum = int.MaxValue;
    }
}
