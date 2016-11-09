namespace CTI;
        // class declarations
         class BuiltInSchedulerExample;
     class BuiltInSchedulerExample 
    {
        // class delegates
        delegate FUNCTION SetRelay ( INTEGER rlyNo );

        // class events

        // class functions
        FUNCTION Clear ();
        FUNCTION Ack ();
        FUNCTION InitializeStuff ();
        STRING_FUNCTION ToString ();
        SIGNED_LONG_INTEGER_FUNCTION GetHashCode ();

        // class variables
        INTEGER __class_id__;

        // class properties
        DelegateProperty SetRelay SetRelayDelegate;
    };

