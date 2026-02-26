using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MS2S.VirtopsiaVR
{
    [Serializable]
    public class User
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class MaxNumUser
    {
        public ulong id;
        public string photonMaxRoomPlayers;
    }

    [Serializable]
    public class Achievements
    {
        public ulong id;
        public string username;
        public string identification;
        public string firstName;
        public string lastName;
        public string type;
        public string role;
        public string genere;
        public string permissions;
        public string aplicative;
        public string laboratory;
        public string scene;
        public string typeApp;
        public string action;
        public string log;
        public string idlog;
        public string time;
    }

    [Serializable]
    public class UserData
    {
        public const string GuestType = "guest";
        public const string UtplType = "utpl";
        public const string ExternalType = "external";

        public ulong id;
        public string firstName;
        public string lastName;
        public string fullName;
        public string identification;
        public string username;
        public string type;
        public string createdDate;
        public string lastLogin;
        public bool rolePermissionsUpdated;
        public bool active;
        public Role role;
        public Permissions[] permissions;
        public string[] academicPrograms;
        public string genere;
        public string labRoomName = "no aplica";
    }

    [Serializable]
    public class DataCompUTPL
    {
        public ulong id;
        public string cedula;
        public string llave;
        public string rol;
        public string modalidad;
        public string nivel;
        public string materiaCodigo;
        public string materiaNombre;
        public string paraleloCodigo;
        public string paraleloNombre;
    }

    [Serializable]
    public class Role
    {
        public const string Guest = "visitante";
        public const string Teacher = "docente";
        public const string Student = "estudiante";
        public const string Admin = "administrador";

        public ulong id;
        public string name;
        public string code;
    }

    [Serializable]
    public class Permissions
    {
        public const string Medicine = "medicina";
        public const string Law = "derecho";
        public const string Management = "gestion";

        public ulong id;
        public string name;
        public string code;
        public bool editable;
    }

    [Serializable]
    public class Tracking   // sfcarrion@utpl.edu.ec
    {
        public ulong id;
        public string name;
        public string token;
    }
}
