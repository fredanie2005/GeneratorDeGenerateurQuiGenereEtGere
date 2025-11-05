using System;
using Unity.VisualScripting;
using UnityEngine;
using VTools.RandomService;

public class RoomNode
{
    private RoomNode firstChild;
    private RoomNode secondChild;
    public RectInt size = new RectInt(0,0,64,64);
    private float spacing = 0.3f; //30%

    [NonSerialized] protected RandomService randomService;

    public RoomNode(RandomService _randomService)
    {
       randomService = _randomService;
    }


    public RoomNode FirstChild
    {
        get { return firstChild; }
    }
    public RoomNode SecondChild
    {
        get { return secondChild; }
    }


    public void Split()
    {
        const int minSize = 6;

        // Trop petit pour splitter
        if (size.width <= minSize * 2 && size.height <= minSize * 2)
            return;

        bool horizontalSplit;

        // Forcer le sens du split selon les proportions
        if (size.width / (float)size.height >= 1.25f)
            horizontalSplit = false; // Split vertical
        else if (size.height / (float)size.width >= 1.25f)
            horizontalSplit = true; // Split horizontal
        else
            horizontalSplit = randomService.Chance(0.5f);

        firstChild = new RoomNode(randomService);
        secondChild = new RoomNode(randomService);

        if (horizontalSplit)
        {
            int minSplit = size.y + minSize;
            int maxSplit = size.yMax - minSize;
            if (maxSplit <= minSplit) return;

            int splitY = randomService.Range(minSplit, maxSplit);
            firstChild.size = new RectInt(size.x, size.y, size.width, splitY - size.y);
            secondChild.size = new RectInt(size.x, splitY, size.width, size.yMax - splitY);
        }
        else
        {
            int minSplit = size.x + minSize;
            int maxSplit = size.xMax - minSize;
            if (maxSplit <= minSplit) return;

            int splitX = randomService.Range(minSplit, maxSplit);
            firstChild.size = new RectInt(size.x, size.y, splitX - size.x, size.height);
            secondChild.size = new RectInt(splitX, size.y, size.xMax - splitX, size.height);
        }
    }



}
