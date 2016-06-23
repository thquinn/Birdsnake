// TODO
//    - Only allow birds to teleport if they weren't in the same square with the teleporter in the previous state or during a fall.
//    - If a bird falls or is pushed into an exit, remove it.
//    - We need a proper search strategy, akin to an A* only we don't actually care about finding the shortest path. Heuristic: minimize fruits, then summed distance of all bird coors to exit.
//    - Store coordinates as single numbers instead of tuples?

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakebirdSolver
{
    enum Block
    {
        EMPTY, PLATFORM, FRUIT, SPIKE, TELEPORTER, EXIT
    }

    class State
    {
        // Set this flag to false if you're sure no objects should fall off an edge during the correct solution.
        static bool OBJECTS_CAN_DIE = false;

        public static StringBuilder sb = new StringBuilder();
        public Block[,] level;
        public List<Bird> birds;
        public bool win;

        public State(string levelString)
        {
            level = ParseLevel(levelString);
            birds = new List<Bird>();
            birds.Add(new Bird(levelString, "1234567890+-"));
            birds.Add(new Bird(levelString, "ABCDEFGHIJKL"));
            birds.Add(new Bird(levelString, "abcdefghijkl"));
            birds.Add(new Bird(levelString, '%'));
            birds.Add(new Bird(levelString, '$'));
            birds.Add(new Bird(levelString, '&'));
            birds.Add(new Bird(levelString, '#'));
            birds.RemoveAll(bird => bird.coor.Count == 0);
            win = false;
        }

        public State(State other)
        {
            level = new Block[other.level.GetLength(0), other.level.GetLength(1)];
            Array.Copy(other.level, 0, level, 0, other.level.Length);
            birds = new List<Bird>();
            foreach (Bird otherBird in other.birds)
                birds.Add(new Bird(otherBird));
            win = false;
        }

        public List<State> Children()
        {
            List<State> children = new List<State>();
            for (int i = 0; i < birds.Count; i++)
            {
                Bird bird = birds[i];
                if (bird.isObject)
                    continue;
                int x = bird.coor[0].Item1;
                int y = bird.coor[0].Item2;
                // Left.
                if (x > 0 && !bird.Contains(x - 1, y))
                    children.Add(Move(i, -1, 0));
                // Right.
                if (x < level.GetLength(0) - 1 && !bird.Contains(x + 1, y))
                    children.Add(Move(i, 1, 0));
                // Up.
                if (y > 0 && !bird.Contains(x, y - 1))
                    children.Add(Move(i, 0, -1));
                // Down.
                if (y < level.GetLength(1) - 2 && !bird.Contains(x, y + 1))
                    children.Add(Move(i, 0, 1));
            }

            children.RemoveAll(child => child == null);
            return children;
        }

        public State Move(int birdIndex, int dx, int dy)
        {
            State copy = new State(this);
            Bird bird = copy.birds[birdIndex];
            // Check for fruit eat.
            Tuple<int, int> headC = bird.coor[0];
            bool fruitEat = false;
            if (copy.level[headC.Item1 + dx, headC.Item2 + dy] == Block.FRUIT)
            {
                fruitEat = true;
                copy.level[headC.Item1 + dx, headC.Item2 + dy] = Block.EMPTY;
            }
            // Check for win.
            if (copy.level[headC.Item1 + dx, headC.Item2 + dy] == Block.EXIT && copy.NoFruits())
            {
                copy.birds.RemoveAt(birdIndex);
                if (copy.NoBirds())
                {
                    copy.win = true;
                    return copy;
                }
            }

            bool validMove = bird.Move(copy, dx, dy, fruitEat);
            if (!validMove)
                return null;
            bool safeFall = copy.FallAll();
            if (!safeFall)
                return null;
            return copy;
        }

        public bool FallAll()
        {
            // Fall all birds at once. When one or more birds land on a surface, remove them from the falling group. Repeat until the falling group is empty.
            List<Bird> fallingBirds = new List<Bird>(birds);
            while (fallingBirds.Count > 0)
            {
                // Check for teleporting birds.
                foreach (Bird bird in fallingBirds)
                    foreach (Tuple<int, int> c in bird.coor)
                        if (level[c.Item1, c.Item2] == Block.TELEPORTER)
                        {
                            // Find the other teleporter.
                            int tx = -1, ty = -1;
                            for (int x = 0; x < level.GetLength(0) && tx == -1; x++)
                                for (int y = 0; y < level.GetLength(1) - 1; y++)
                                    if (x == c.Item1 && y == c.Item2)
                                        continue;
                                    else if (level[x, y] == Block.TELEPORTER)
                                    {
                                        tx = x;
                                        ty = y;
                                        break;
                                    }
                            // Check all coordinates to make see if the other side of the teleporter is empty.
                            bool tpClear = true;
                            foreach (Tuple<int, int> cCheck in bird.coor)
                            {
                                int dx = cCheck.Item1 - c.Item1;
                                int dy = cCheck.Item2 - c.Item2;
                                if (IsSolid(level[tx + dx, ty + dy]))
                                {
                                    tpClear = false;
                                    break;
                                }
                            }
                            if (tpClear)
                            {
                                int shiftX = tx - c.Item1;
                                int shiftY = ty - c.Item2;
                                for (int i = 0; i < bird.coor.Count; i++)
                                    bird.coor[i] = new Tuple<int, int>(bird.coor[i].Item1 + shiftX, bird.coor[i].Item2 + shiftY);
                            }
                            break;
                        }

                // Kill anything in the bottom row.
                // We shouldn't have to do this check again here, but it's not working without it, so...
                for (int i = birds.Count - 1; i >= 0; i--)
                {
                    Bird bird = birds[i];
                    foreach (Tuple<int, int> c in bird.coor)
                        if (c.Item2 >= level.GetLength(1) - 1)
                            if (!bird.isObject || !OBJECTS_CAN_DIE)
                                return false;
                            else
                            {
                                fallingBirds.Remove(bird);
                                birds.Remove(bird);
                                break;
                            }
                }
                
                // Check for landed birds.
                bool fall = true;
                for (int i = fallingBirds.Count - 1; i >= 0; i--)
                {
                    Bird fallingBird = fallingBirds[i];
                    foreach (Tuple<int, int> c in fallingBird.coor)
                        if (IsSolid(level[c.Item1, c.Item2 + 1]) || AreOtherBirdsAt(fallingBirds, c.Item1, c.Item2 + 1))
                        {
                            fallingBirds.Remove(fallingBird);
                            fall = false;
                            break;
                        }
                }
                if (fall)
                {
                    // Fall every falling bird.
                    foreach (Bird fallingBird in fallingBirds)
                        for (int i = 0; i < fallingBird.coor.Count; i++)
                            fallingBird.coor[i] = new Tuple<int, int>(fallingBird.coor[i].Item1, fallingBird.coor[i].Item2 + 1);
                    // Kill anything in the bottom row.
                    for (int i = birds.Count - 1; i >= 0; i--)
                    {
                        Bird bird = birds[i];
                        foreach (Tuple<int, int> c in bird.coor)
                            if (c.Item2 >= level.GetLength(1) - 1)
                                if (!bird.isObject || !OBJECTS_CAN_DIE)
                                    return false;
                                else
                                {
                                    fallingBirds.Remove(bird);
                                    birds.Remove(bird);
                                    break;
                                }
                    }
                }
            }

            // If there are any real birds sitting only on spikes, we lose.
            foreach (Bird bird in birds)
            {
                if (bird.isObject)
                    continue;
                bool safe = false;
                foreach (Tuple<int, int> c in bird.coor)
                {
                    Block surface = level[c.Item1, c.Item2 + 1];
                    if (surface == Block.PLATFORM || surface == Block.FRUIT || IsOtherBirdAt(bird, c.Item1, c.Item2 + 1))
                        safe = true;
                }
                if (!safe)
                    return false;
            }

            return true;
        }

        public bool IsOtherBirdAt(Bird bird, int x, int y)
        {
            return AreOtherBirdsAt(new List<Bird>(new Bird[] { bird }), x, y);
        }

        public bool AreOtherBirdsAt(IEnumerable<Bird> ourBirds, int x, int y)
        {
            foreach (Bird other in birds)
            {
                if (ourBirds.Contains(other))
                    continue;
                if (other.Contains(x, y))
                    return true;
            }
            return false;
        }

        public Bird GetBirdAt(int x, int y)
        {
            foreach (Bird bird in birds)
                if (bird.Contains(new Tuple<int, int>(x, y)))
                    return bird;
            return null;
        }

        public bool NoFruits()
        {
            for (int x = 0; x < level.GetLength(0); x++)
                for (int y = 0; y < level.GetLength(1); y++)
                    if (level[x, y] == Block.FRUIT)
                        return false;
            return true;
        }

        public bool NoBirds()
        {
            foreach (Bird bird in birds)
                if (!bird.isObject)
                    return false;
            return true;
        }

        public bool Within(Tuple<int, int> c)
        {
            if (c.Item1 < 0 || c.Item1 >= level.GetLength(0))
                return false;
            if (c.Item2 < 0 || c.Item2 >= level.GetLength(1))
                return false;
            return true;
        }

        public bool NoOverlaps()
        {
            HashSet<Tuple<int, int>> occupied = new HashSet<Tuple<int, int>>();
            foreach (Bird bird in birds)
                foreach (Tuple<int, int> c in bird.coor)
                    if (occupied.Contains(c))
                        return false;
                    else
                        occupied.Add(c);
            return true;
        }

        public override string ToString()
        {
            sb.Clear();
            for (int y = 0; y < level.GetLength(1) - 1; y++)
            {
                for (int x = 0; x < level.GetLength(0); x++)
                {
                    bool birded = false;
                    for (int j = 0; j < birds.Count; j++)
                        for (int i = 0; i < birds[j].coor.Count; i++)
                            if (birds[j].coor[i].Item1 == x && birds[j].coor[i].Item2 == y)
                            {
                                if (birds[j].isObject)
                                    sb.Append(birds[j].objectChar);
                                else
                                    sb.Append(BirdChar(j, i));
                                birded = true;
                            }
                    if (!birded)
                        switch (level[x, y])
                        {
                            case Block.EMPTY:
                                sb.Append('.');
                                break;
                            case Block.PLATFORM:
                                sb.Append('=');
                                break;
                            case Block.FRUIT:
                                sb.Append('*');
                                break;
                            case Block.SPIKE:
                                sb.Append('X');
                                break;
                            case Block.TELEPORTER:
                                sb.Append('O');
                                break;
                            case Block.EXIT:
                                sb.Append('@');
                                break;
                        }
                }
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is State))
                return false;
            State other = (State)obj;
            return GetHashString() == other.GetHashString();
        }

        public override int GetHashCode()
        {
            return GetHashString().GetHashCode();
        }

        public string GetHashString()
        {
            List<string> strings = new List<string>();
            foreach (Bird bird in birds)
                strings.Add(bird.ToString());
            strings.Sort();
            for (int y = 0; y < level.GetLength(1); y++)
                for (int x = 0; x < level.GetLength(0); x++)
                    if (level[x, y] == Block.FRUIT)
                        strings.Add("fruit(" + x + ',' + y + ')');
            return string.Join<string>("", strings);
        }

        static char BirdChar(int bird, int segment)
        {
            return new string[] {
                "1234567890+------------------",
                "ABCDEFGHIJKL",
                "abcdefghijkl",
            }[bird][segment];
        }

        static Block[,] ParseLevel(string levelString)
        {
            List<string> lines = new List<string>(levelString.Split('/'));
            lines.Add(new string('.', lines[0].Length));

            Block[,] level = new Block[lines[0].Length, lines.Count];
            foreach (string line in lines)
                if (line.Length != lines[0].Length)
                    throw new Exception("Jagged level string!");
            for (int x = 0; x < level.GetLength(0); x++)
                for (int y = 0; y < level.GetLength(1); y++)
                    switch (lines[y][x])
                    {
                        case '=':
                            level[x, y] = Block.PLATFORM;
                            break;
                        case '*':
                            level[x, y] = Block.FRUIT;
                            break;
                        case 'X':
                            level[x, y] = Block.SPIKE;
                            break;
                        case 'O':
                            level[x, y] = Block.TELEPORTER;
                            break;
                        case '@':
                            level[x, y] = Block.EXIT;
                            break;
                        default:
                            level[x, y] = Block.EMPTY;
                            break;
                    }
            return level;
        }

        public static bool IsSolid(Block block)
        {
            return block == Block.PLATFORM || block == Block.FRUIT || block == Block.SPIKE;
        }

        public static bool CanMoveInto(Block block)
        {
            return block == Block.EMPTY || block == Block.FRUIT || block == Block.EXIT;
        }
    }

    class Bird
    {
        public static StringBuilder sb = new StringBuilder();
        public bool isObject;
        public char objectChar;
        public List<Tuple<int, int>> coor;

        public Bird(string levelString, string birdChars)
        {
            isObject = false;
            objectChar = '~';
            coor = new List<Tuple<int, int>>();
            string[] lines = levelString.Split('/');
            int num = 0;
            bool found = true;
            while (found)
            {
                found = false;
                for (int x = 0; x < lines[0].Length; x++)
                    for (int y = 0; y < lines.Length; y++)
                        if (lines[y][x] == birdChars[num])
                        {
                            coor.Add(new Tuple<int, int>(x, y));
                            found = true;
                        }
                num++;
            }
        }

        public Bird(string levelString, char objectChar)
        {
            isObject = true;
            this.objectChar = objectChar;
            coor = new List<Tuple<int, int>>();
            string[] lines = levelString.Split('/');
            for (int x = 0; x < lines[0].Length; x++)
                for (int y = 0; y < lines.Length; y++)
                    if (lines[y][x] == objectChar)
                        coor.Add(new Tuple<int, int>(x, y));
        }

        public Bird(Bird other)
        {
            isObject = other.isObject;
            objectChar = other.objectChar;
            coor = new List<Tuple<int, int>>();
            foreach (Tuple<int, int> c in other.coor)
                coor.Add(new Tuple<int, int>(c.Item1, c.Item2));
        }

        public bool Move(State state, int dx, int dy, bool fruitEat)
        {
            int x = coor[0].Item1 + dx;
            int y = coor[0].Item2 + dy;
            if (!fruitEat)
                coor.RemoveAt(coor.Count - 1);
            coor.Insert(0, new Tuple<int,int>(x, y));

            // Push other birds.
            HashSet<Bird> pushedBirds = new HashSet<Bird>();
            pushedBirds.Add(this);
            List<Bird> birdsToPush = new List<Bird>();
            foreach (Bird bird in state.birds)
                if (birdsToPush.Count > 0)
                    break;
                else if (bird == this)
                    continue;
                else if (bird.Contains(coor[0]))
                    birdsToPush.Add(bird);
            while (birdsToPush.Count > 0)
            {
                Bird bird = birdsToPush[0];
                birdsToPush.RemoveAt(0);
                pushedBirds.Add(bird);
                for (int i = 0; i < bird.coor.Count; i++)
                {
                    bird.coor[i] = new Tuple<int, int>(bird.coor[i].Item1 + dx, bird.coor[i].Item2 + dy);
                    // Make sure we're somewhere within the confines of the level.
                    if (!state.Within(bird.coor[i]))
                        return false;
                }
                // Check to see if we just pushed another bird.
                foreach (Bird other in state.birds)
                {
                    if (pushedBirds.Contains(other))
                        continue;
                    HashSet<Tuple<int, int>> coorHash = new HashSet<Tuple<int, int>>(bird.coor);
                    foreach (Tuple<int, int> otherC in other.coor)
                        if (coorHash.Contains(otherC))
                        {
                            birdsToPush.Add(other);
                            break;
                        }
                }
            }
            foreach (Bird bird in pushedBirds)
                foreach (Tuple<int, int> c in bird.coor)
                    if (State.IsSolid(state.level[c.Item1, c.Item2]))
                        return false;
            if (!state.NoOverlaps())
                return false;
            return true;
        }

        public bool Contains(Tuple<int, int> c)
        {
            return Contains(c.Item1, c.Item2);
        }
        public bool Contains(int x, int y)
        {
            foreach (Tuple<int, int> c in coor)
                if (c.Item1 == x && c.Item2 == y)
                    return true;
            return false;
        }

        public override string ToString()
        {
            sb.Clear();
            sb.Append(isObject ? "object" : "bird");
            sb.Append(": ");
            foreach (Tuple<int, int> c in coor)
            {
                sb.Append('(');
                sb.Append(c.Item1);
                sb.Append(',');
                sb.Append(c.Item2);
                sb.Append(')');
            }
            return sb.ToString();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // . empty
            // = platform
            // * fruit
            // X spike
            // O teleporter
            // @ exit
            // 1234567890+- first bird
            // ABCDEFGHIJKL second bird
            // abcdefghijkl third bird
            // % first object
            // $ second object
            // & third object
            // # fourth object
            // / row delimiter

            // Simple Test:
            //string levelString = "........./........./4321BA..@/======XX=";
            // Spike + Object Test:
            //string levelString = "......../.%21...@/====..==/====XX==/========";
            // Partial Solve Test:
            //string levelString = ".........../.........../...1......@/..32%%...../..4$$....../..5=......./...A......./..CB......./..D=......./..E......../...=.......";
            // Partial Solve Test 2:
            //string levelString = ".........@/.X......../...X....../...21X..../.DCBA...../.X====..X=/..====...=";
            // Partial Solve Test 3:
            //string levelString = "......@...../............/............/............/......X...../.$.%.$.%..../.....1.X..../..&CB2&...../....A3X...../..=========./....=====...";
            // Gravity Test (should be impossible):
            //string levelString = "...../....@/..21./====./....%/....A/....%/=====";
            // Level 1:
            //string levelString = "...@../....../=....=/*..=*=/....../.=21../.====.";
            // Level 7:
            //string levelString = "............/........@.../............/.....=....../............/..=........./..=........./..=..=...123/.....=..ABC4/........====";
            // Level 12:
            //string levelString = "....X......./...5.1....../...432....../....=......./.........X../...........@/.X=X..XXX=../........==../............/.*...X....../....=X*...../....===...../....===.....";
            // Level 13:
            //string levelString = ".............../....XXXX......./....===X......./....===X...=.../...........=.../..@.......123../...........=4../...........=.../===.......ABC../.===.......=D../...........=E../...........=...";
            // Level 14:
            //string levelString = ".........../........X../.@...21..../.....BA.=../.....C=.=../..=...=.=../..=...=.=../..=...=.=..";
            // Level 17:
            //string levelString = "............./......=X...../......=X...../.......X123../........BA4../..==X..X==5../..==XX.X==.../...=X..X=..../.......@...../.......X...../......=X...../......=X...../.....XXX.....";
            // Level 18:
            //string levelString = "............/............/......==..../......==..../......==..../.......=..../@......=..../...=.X...123/.....=.=ABC4/..=..=.====./........==../...===....../....==....../.....==X....";
            // Level 19:
            //string levelString = "...X.........../X..=.........../=.@..........*./...12........../..CBA........../..Dab........../..E=c........../..F=d........../...=e..........";
            // Level 22:
            //string levelString = ".........../....@....../.........../.........../.........../.........../.321......./..==...%%../..==.=.%%../..==...==../..=======..";
            // Level 23:
            //string levelString = "====....=.../====....=.../........X.../+.......XXXX/098...%.====/567...$$$.==/4321....====/======.=====/======%=====/..=@X=.....=/..=.X=..=..=/..=.X......=/..=.X......=/..=.X.X....=/..=...=....=";
            // Level 24:
            //string levelString = "...*.../...=.../......./......./......./......./.%%%.3./.%%%12@/..%.AB./..===C.";
            // Level 25:
            //string levelString = ".........../.........../.....@...../.........../.....=...../.........../.........../.........../.........../.....%...../....===..../...%...%.../.........../..21.%.AB../..3=...=C../..4=...=.../...=...=...";
            // Level 26:
            //string levelString = ".........X==/.........X../...*.....X.@/12........../.A.......X==/.B.......X==/.C%......X==/.=XXXXXXXX==";
            // Level 27:
            //string levelString = "....*..../...==..../...==..../...==..../........./........./.X....X../.==..==../.X..%.X../........./.21.%.AB@/.======C./.======D.";
            // Level 28:
            //string levelString = ".....@..../........../........../..=.%...../..===..X../....*=..../.....=..../..X=%...../.....321../....=ABC../...=====../...=====..";
            // Level 29:
            //string levelString = "......@.../........../........../........../........../........../...123..../...%%...../...%%...../.$$&&##..C/.$$&&##.AB/.=====..==/.=========";
            // Level 35:
            //string levelString = ".=====../......../.....=../.....X../......*./...O.=../.....=../......../@.321.../..4=..../...=..../....O.../...XX.../...*..../...==.../...==...";
            // Level 39:
            //string levelString = ".........@/........../........../.......===/.......===/........==/.......===/.321...===/.4%%....==/.5$$...===/====..====/==========";
            // Level 40:
            //string levelString = ".........../.........../..........@/.........../.........../...=......./....%%...../...$$....../...=......./...54....../...=321..../..ABCDE..../...=......./...=.......";
            // Level 41:
            //string levelString = "............/............/......@...../............/............/..21....AB../..3*....*C../.....=....../.....=....../.....=......";
            // Level 42:
            //string levelString = ".......XX=./.......===./........=../...=.21.=.@/.=...AB..../...*X*=..=./..X......=./..==.=...=.";
            // Level 43:
            //string levelString = "...==...../...===..../...===..@./....=...../...123..../.X.CBA.X../...%*%..../...%%%..=./...=X=..=.";
            // Level 45:
            string levelString = "........./....%..../...X$..../....$..../..=.=...@/..X....../34X....../21..XABC./=XX*.==../==...==../=.....=../...*...../........./........./...=...../..X=X....";
            // Level *1:
            //string levelString = "........=XXXXX......../........=............./........=............./........=.XXXX......../..@..........X......../....................../...............123.=../.===............%4..../====................../====...........ABC..=./.==............abcd.../................%...../...............=====..";
            // Level *2:
            //string levelString = ".=======....../.=.*.*.==...../==*=*=*==.===./=*******======/=.=*=*=.123.@=/=*******=====./==*=*=*====.../.=.*.*.=....../..======......";
            // Level *3:
            //string levelString = "..........=..../..........=..../@........%%%..X/.........%1%.../.........%2%.../..........3..../........BA4...X/....=...Ccba=XX/...==....X=XX../.............../...==........../.......=.......";
            // Level *4:
            //string levelString = "......@....../............./............./............./......X....../....%...%..../....$..X$..../....&...&..../...123XCBA.../..=========..";
            // Level *6:
            //string levelString = "............/............/......@...../............/......=...../..%%......../..=.....=.../..==....=.../........==../..$$......../..==......../..==.&&&==../..==.....=../..123&&&..../..CBA==abc../..========../..=======...";
            State start = new State(levelString);
            Console.WriteLine(start);
            Console.WriteLine("Starting search...");
            Console.WriteLine();

            // Search.
            Dictionary<State, State> stateParents = new Dictionary<State, State>();
            stateParents.Add(start, null);
            Queue<State> queue = new Queue<State>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                // Search.
                State state = queue.Dequeue();
                foreach (State child in state.Children())
                {
                    if (stateParents.ContainsKey(child))
                        continue;
                    stateParents.Add(child, state);
                    queue.Enqueue(child);
                    // Progress printing.
                    if (stateParents.Count % 1000 == 0)
                    {
                        Console.WriteLine(state);
                        Console.WriteLine("Searched " + stateParents.Count + " nodes...");
                        Console.WriteLine();
                    }

                    // We won!
                    if (child.win)
                    {
                        List<State> winPath = new List<State>();
                        State node = child;
                        while (node != null)
                        {
                            winPath.Add(node);
                            node = stateParents[node];
                        }
                        winPath.Reverse();
                        Console.Clear();
                        foreach (State nodePrint in winPath)
                        {
                            Console.WriteLine(nodePrint);
                            Console.WriteLine();
                        }
                        Console.WriteLine("Path of length " + winPath.Count + " found. Searched " + stateParents.Count + " nodes.");
                        Console.ReadKey();
                        return;
                    }
                }
            }

            Console.WriteLine("No path found. Searched " + stateParents.Count + " nodes.");
            Console.ReadKey();
        }
    }
}