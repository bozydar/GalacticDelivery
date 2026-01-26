namespace GalacticDelivery.Db;

public static class Schema
{
    public const string V1 = """
                                 CREATE TABLE IF NOT EXISTS Drivers (
                                      Id TEXT PRIMARY KEY,
                                      FirstName TEXT NOT NULL,
                                      LastName TEXT NOT NULL,
                                      CurrentTripId TEXT NULL
                                 );

                                 CREATE TABLE IF NOT EXISTS Vehicles (
                                      Id TEXT PRIMARY KEY,
                                      RegNumber TEXT NOT NULL,
                                      CurrentTripId TEXT NULL
                                 );

                                 CREATE TABLE IF NOT EXISTS Routes (
                                      Id TEXT PRIMARY KEY,
                                      Origin TEXT NOT NULL,
                                      Destination TEXT NOT NULL,
                                      Checkpoints TEXT NOT NULL
                                 );

                                 CREATE TABLE IF NOT EXISTS Trips (
                                     Id TEXT PRIMARY KEY,
                                     CreatedAt DATETIME NOT NULL,
                                     RouteId TEXT NOT NULL,
                                     VehicleId TEXT NOT NULL,
                                     DriverId TEXT NOT NULL,
                                     Status TEXT NOT NULL,
                                     FOREIGN KEY (RouteId) REFERENCES Routes(Id),
                                     FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id),
                                     FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
                                 );

                                 CREATE TABLE IF NOT EXISTS Events (
                                     Id TEXT PRIMARY KEY,
                                     CreatedAt DATETIME NOT NULL,
                                     TripId TEXT NOT NULL,
                                     Type TEXT NOT NULL,
                                     Payload TEXT NOT NULL,
                                     FOREIGN KEY (TripId) REFERENCES Trips(Id)
                                 );

                                 CREATE INDEX IF NOT EXISTS IDX_Events_TripId ON Events(TripId);

                                 CREATE TABLE TripReports (
                                   TripId TEXT PRIMARY KEY,
                                   GeneratedAt TEXT NOT NULL,
                                   CreatedAt TEXT NOT NULL,
                                   StartedAt TEXT NULL,
                                   CompletedAt TEXT NULL,
                                   DurationSeconds INTEGER NULL,
                                   DriverId TEXT NOT NULL,
                                   DriverName TEXT NOT NULL,
                                   VehicleId TEXT NOT NULL,
                                   VehicleRegistrationNumber TEXT NOT NULL,
                                   RouteId TEXT NOT NULL,
                                   RouteOrigin TEXT NOT NULL,
                                   RouteDestination TEXT NOT NULL,
                                   CheckpointsPlanned TEXT NOT NULL,
                                   CheckpointsPassed TEXT NOT NULL,
                                   IncidentsCount INTEGER NOT NULL
                                 );
                                 
                                 CREATE TABLE TripReportEvents (
                                   Id TEXT PRIMARY KEY,
                                   TripId TEXT NOT NULL,
                                   CreatedAt TEXT NOT NULL,
                                   Type TEXT NOT NULL,
                                   Payload TEXT NULL
                                 );
                                 """;
}
