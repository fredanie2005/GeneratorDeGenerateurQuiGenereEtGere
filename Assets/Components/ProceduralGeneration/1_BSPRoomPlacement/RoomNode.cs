using System;
using UnityEngine;
using VTools.RandomService;

public class RoomNode
{
    private RoomNode firstChild;
    private RoomNode secondChild;
    public RectInt size;
    private float spacing = 0.3f; // unused for now but kept

    [NonSerialized] protected RandomService randomService;

    public RoomNode(RandomService _randomService, RectInt initialSize)
    {
        randomService = _randomService ?? throw new ArgumentNullException(nameof(_randomService));
        size = initialSize;
    }

    public RoomNode FirstChild => firstChild;
    public RoomNode SecondChild => secondChild;

    /// <summary>
    /// Returns true if the node was successfully split into two children.
    /// </summary>
    public bool Split()
    {
        const int minPartitionSize = 6;

        // If already splitted, do nothing
        if (firstChild != null || secondChild != null)
            return true;

        // Too small to split
        if (size.width <= minPartitionSize * 2 && size.height <= minPartitionSize * 2)
            return false;

        bool horizontalSplit;

        // Force split direction based on proportions
        if (size.width / (float)size.height >= 1.25f)
            horizontalSplit = false; // vertical split (split X)
        else if (size.height / (float)size.width >= 1.25f)
            horizontalSplit = true; // horizontal split (split Y)
        else
            horizontalSplit = randomService.Chance(0.5f);

        // Create child nodes
        firstChild = new RoomNode(randomService, new RectInt(0, 0, 0, 0));
        secondChild = new RoomNode(randomService, new RectInt(0, 0, 0, 0));

        if (horizontalSplit)
        {
            int minSplit = size.y + minPartitionSize;
            int maxSplit = size.yMax - minPartitionSize;
            if (maxSplit <= minSplit)
            {
                // can't split horizontally
                firstChild = null;
                secondChild = null;
                return false;
            }

            int splitY = randomService.Range(minSplit, maxSplit);
            firstChild.size = new RectInt(size.x, size.y, size.width, splitY - size.y);
            secondChild.size = new RectInt(size.x, splitY, size.width, size.yMax - splitY);
        }
        else
        {
            int minSplit = size.x + minPartitionSize;
            int maxSplit = size.xMax - minPartitionSize;
            if (maxSplit <= minSplit)
            {
                // can't split vertically
                firstChild = null;
                secondChild = null;
                return false;
            }

            int splitX = randomService.Range(minSplit, maxSplit);
            firstChild.size = new RectInt(size.x, size.y, splitX - size.x, size.height);
            secondChild.size = new RectInt(splitX, size.y, size.xMax - splitX, size.height);
        }

        return true;
    }

    /// <summary>
    /// Checks whether the partition has the potential to be split further.
    /// </summary>
    public bool CanSplit()
    {
        const int minPartitionSize = 6;
        return size.width > minPartitionSize * 2 || size.height > minPartitionSize * 2;
    }
}
