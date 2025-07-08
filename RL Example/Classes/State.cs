using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Classes
{
    public class State
    {
        private string name;
        private double posX;
        private double posZ;

        public State()
        {
            name = "";
            posX = 0;
            posZ = 0;
        }

        public State(string name, double posX, double posZ)
        {
            name = "";
            this.posX = posX;
            this.posZ = posZ;
        }

        public string GetName()
        {
            return name;
        }

        public void GetCoordinates(ref double posX, ref double posZ)
        {
            posX = this.posX;
            posZ = this.posZ;
        }
    }
}
