using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace Pokemon_Server
{
    class Player
    {
        public int id;
        public string username;

        public Vector2 position;
        public Quaternion rotation;

        private float moveSpeed = 5f / Constants.TICKS_PER_SEC;
        private bool[] inputs;

        public Player(int _id, string _username, Vector2 _spawnPosition)
        {
            id = _id;
            username = _username;
            position = _spawnPosition;
            rotation = Quaternion.Identity;

            inputs = new bool[4];
        }

        public void Update()
        {
            Vector2 _inputDirection = Vector2.Zero;
            if (inputs[0])
            {
                _inputDirection.Y += 1;
            }
            if (inputs[1])
            {
                _inputDirection.Y -= 1;
            }
            if (inputs[2])
            {
                _inputDirection.X += 1;
            }
            if (inputs[3])
            {
                _inputDirection.X -= 1;
            }

            Move(_inputDirection);
        }


        //no clue how to do this
        public void Move(Vector2 _inputDirection)
        {
            Vector2 _forward = Vector2.Transform(new Vector2(0, 1), Quaternion.Identity);
            Vector2 _right = Vector2.Transform(new Vector2(1, 0), Quaternion.Identity);

            Vector2 _moveDirection = _right * _inputDirection.X + _forward * _inputDirection.Y;
            position += _moveDirection * moveSpeed;

            ServerSend.PlayerPosition(this);

        }
        public void SetInput(bool[] _inputs)
        {
            inputs = _inputs;
        }
    }
}
