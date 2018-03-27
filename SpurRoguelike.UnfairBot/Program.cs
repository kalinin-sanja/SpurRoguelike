using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Entities;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.UnfairBot
{
    public class UnfairBot : IPlayerController
    {
        private LevelView _levelView;
        private HashSet<Location> _visitedCells = new HashSet<Location>();
        private Location _start;
        private Location _zero;

        private Location Left => new Location(_levelView.Player.Location.X - 1, _levelView.Player.Location.Y);
        private Location Right => new Location(_levelView.Player.Location.X + 1, _levelView.Player.Location.Y);
        private Location Up => new Location(_levelView.Player.Location.X, _levelView.Player.Location.Y - 1);
        private Location Down => new Location(_levelView.Player.Location.X, _levelView.Player.Location.Y + 1);

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            var type = typeof(LevelView);
            var types = type.Assembly.GetTypes();
            var plr = (Pawn)type.GetProperty("Player")
                .PropertyType.GetField("pawn",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(levelView.Player);


            types.FirstOrDefault(x => x.Name == "Pawn")
                .GetProperty("Health")
                .SetValue(plr, 100);

            messageReporter.ReportMessage("Hey ho! I'm still breathing");

            _levelView = levelView;
            var start = levelView.Field.GetCellsOfType(CellType.PlayerStart).FirstOrDefault();

            if (start != _zero && start != _start)
            {
                _start = start;
                _visitedCells = new HashSet<Location>();
            }

            PawnView nearbyMonster;

            var directions = new HashSet<StepDirection>();
            StepDirection direction = StepDirection.East;

            nearbyMonster =
                levelView.Monsters.FirstOrDefault(m => IsInAttackRange(levelView.Player.Location, m.Location));

            if (nearbyMonster.HasValue)
                return Turn.Attack(nearbyMonster.Location - levelView.Player.Location);



            while (directions.Count < 4)
            {
                direction = (StepDirection)levelView.Random.Next(4);
                if (directions.Add(direction))
                {
                    var point = GetPoint(direction);
                    var health = levelView.GetHealthPackAt(point);

                    if (health.HasValue/* && CellIsAvailable(point)*/)
                    {
                        nearbyMonster = levelView.Monsters.FirstOrDefault(m => IsInRiskAttackRange(point, m.Location));
                        if (nearbyMonster.HasValue == false)
                        {
                            _visitedCells.Add(point);
                            return Turn.Step(direction);
                        }
                    }
                }
            }


            directions = new HashSet<StepDirection>();
            while (directions.Count < 4)
            {
                direction = (StepDirection)levelView.Random.Next(4);
                if (directions.Add(direction))
                {
                    var point = GetPoint(direction);
                    if (CellIsAvailable(point) && !_visitedCells.Contains(point))
                    {
                        nearbyMonster = levelView.Monsters.FirstOrDefault(m => IsInAttackRange(point, m.Location));
                        if (nearbyMonster.HasValue == false)
                        {
                            _visitedCells.Add(point);
                            return Turn.Step(direction);
                        }
                    }
                }
            }

            directions = new HashSet<StepDirection>();
            while (directions.Count < 4)
            {
                direction = (StepDirection)levelView.Random.Next(4);
                if (directions.Add(direction))
                {
                    var point = GetPoint(direction);
                    if (CellIsAvailable(point))
                    {
                        nearbyMonster = levelView.Monsters.FirstOrDefault(m => IsInAttackRange(point, m.Location));
                        if (nearbyMonster.HasValue == false)
                        {
                            _visitedCells.Add(point);
                            return Turn.Step(direction);
                        }
                    }
                }
            }

            directions = new HashSet<StepDirection>();
            while (directions.Count < 4)
            {
                direction = (StepDirection)levelView.Random.Next(4);
                if (directions.Add(direction))
                {
                    var point = GetPoint(direction);
                    if (CellIsAvailable(point))
                    {
                        nearbyMonster = levelView.GetMonsterAt(point);
                        if (nearbyMonster.HasValue == false)
                        {
                            _visitedCells.Add(point);
                            return Turn.Step(direction);
                        }
                    }
                }
            }

            if (direction == StepDirection.East)
                _visitedCells.Add(Right);
            else if (direction == StepDirection.West)
                _visitedCells.Add(Left);
            else if (direction == StepDirection.North)
                _visitedCells.Add(Up);
            else if (direction == StepDirection.South)
                _visitedCells.Add(Down);

            return Turn.Step(direction);
        }

        private static bool IsInAttackRange(Location a, Location b)
        {
            return a.IsInRange(b, 1);
        }

        private static bool IsInRiskAttackRange(Location a, Location b)
        {
            return a.IsInRange(b, 4);
        }

        private bool CellIsAvailable(Location cell)
        {
            if (_levelView.Field[cell] == CellType.Empty || _levelView.Field[cell] == CellType.PlayerStart ||
                _levelView.Field[cell] == CellType.Exit
                || _levelView.Field[cell] == CellType.Hidden)
                if (!NoWay(cell, _levelView.Player.Location))
                    return true;
            return false;
        }

        private bool NoWay(Location cell, Location player)
        {
            var left = new Location(cell.X - 1, cell.Y);
            var right = new Location(cell.X + 1, cell.Y);
            var up = new Location(cell.X, cell.Y - 1);
            var down = new Location(cell.X, cell.Y + 1);

            if ((_levelView.Field[left] == CellType.Wall || left == player)
                && (_levelView.Field[right] == CellType.Wall || right == player)
                && (_levelView.Field[up] == CellType.Wall || up == player)
                && (_levelView.Field[down] == CellType.Wall || down == player))
                return true;

            return false;
        }

        private Location GetPoint(StepDirection direction)
        {
            switch (direction)
            {
                case StepDirection.East:
                    return Right;
                case StepDirection.West:
                    return Left;
                case StepDirection.North:
                    return Up;
                case StepDirection.South:
                    return Down;
                default:
                    return new Location();
            }
        }
    }
}