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

                                 CREATE TABLE IF NOT EXISTS TripReports (
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
                                 
                                 CREATE TABLE IF NOT EXISTS TripReportEvents (
                                   Id TEXT PRIMARY KEY,
                                   TripId TEXT NOT NULL,
                                   CreatedAt TEXT NOT NULL,
                                   Type TEXT NOT NULL,
                                   Payload TEXT NULL
                                 );
                                 """;

    public const string Seed = """
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('0f4d7c31-06c5-4f8a-8cfe-3b4e3a0d7a9c', 'Nova', 'Starlance', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('3dd9c391-6e3c-4d7c-9b8b-3a3d5f4a5f5b', 'Orion', 'Driftwood', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('c8f3a2f5-2f87-4d36-8b9c-8b8dd0d38b5c', 'Lyra', 'Astra', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('6c9f0b0f-5e53-4f6e-b7bb-5f1f3aab2a2e', 'Cass', 'Quill', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('ef1e47a6-89a6-4e3a-9e1f-6a8e1d7f7e6f', 'Vega', 'Skyrider', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('2f9c0f47-9a2b-4a24-8b0f-8b7bcd1c0bfa', 'Altair', 'Voidwalker', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('d0a19e46-0d2c-48b1-b2d6-6b2f7b36d6d1', 'Rhea', 'Cometborne', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('b1dfc3a0-1b6b-4c0c-9a46-9b8c1d8a5b8d', 'Zara', 'Redshift', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('9df2c5a1-7e84-4a9c-8a3b-9b6f3a1e2a4c', 'Kepler', 'Helios', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('8c7f52e1-24f3-4c41-9e6a-1a2c3d4e5f61', 'Sagan', 'Nightfall', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('2a4b6c8d-1e2f-4a3b-9c8d-1e2f3a4b5c6d', 'Piper', 'Galewing', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('4e5f6a7b-8c9d-4e3f-9a1b-2c3d4e5f6a7b', 'Juno', 'Farsight', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('5a6b7c8d-9e0f-4a1b-9c2d-3e4f5a6b7c8d', 'Mira', 'Cosma', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('6b7c8d9e-0f1a-4b2c-9d3e-4f5a6b7c8d9e', 'Riley', 'Starling', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('7c8d9e0f-1a2b-4c3d-9e4f-5a6b7c8d9e0f', 'Sol', 'Nebulon', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('8d9e0f1a-2b3c-4d5e-9f6a-7b8c9d0e1f2a', 'Aria', 'Kestrel', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('9e0f1a2b-3c4d-4e5f-9a6b-8c9d0e1f2a3b', 'Cyrus', 'Vortex', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('0f1a2b3c-4d5e-4f6a-9b7c-9d0e1f2a3b4c', 'Luna', 'Solaris', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('1a2b3c4d-5e6f-4a7b-9c8d-0e1f2a3b4c5d', 'Nox', 'Wayfarer', NULL);
                                   INSERT OR IGNORE INTO Drivers (Id, FirstName, LastName, CurrentTripId) VALUES ('2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d6e', 'Taryn', 'Orbitson', NULL);

                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('a12b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d', 'GD-001-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('b23c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d6e', 'NOVA-002-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('c34d5e6f-7a8b-4c9d-0e1f-2a3b4c5d6e7f', 'ION-003-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('d45e6f7a-8b9c-4d0e-1f2a-3b4c5d6e7f8a', 'STAR-004-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('e56f7a8b-9c0d-4e1f-2a3b-4c5d6e7f8a9b', 'ORBIT-005-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('f67a8b9c-0d1e-4f2a-3b4c-5d6e7f8a9b0c', 'LUX-006-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('0a7b8c9d-1e2f-4a3b-5c6d-7e8f9a0b1c2d', 'ZEPH-007-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('1b8c9d0e-2f3a-4b5c-6d7e-8f9a0b1c2d3e', 'PULSE-008-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('2c9d0e1f-3a4b-4c5d-6e7f-9a0b1c2d3e4f', 'NEB-009-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('3d0e1f2a-4b5c-4d6e-7f8a-0b1c2d3e4f5a', 'COMET-010-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('4e1f2a3b-5c6d-4e7f-8a9b-1c2d3e4f5a6b', 'QUAS-011-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('5f2a3b4c-6d7e-4f8a-9b0c-2d3e4f5a6b7c', 'RIFT-012-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('6a3b4c5d-7e8f-4a9b-0c1d-3e4f5a6b7c8d', 'AST-013-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('7b4c5d6e-8f9a-4b0c-1d2e-4f5a6b7c8d9e', 'VORT-014-X', NULL);
                                   INSERT OR IGNORE INTO Vehicles (Id, RegNumber, CurrentTripId) VALUES ('8c5d6e7f-9a0b-4c1d-2e3f-5a6b7c8d9e0f', 'LUMA-015-X', NULL);

                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('9d6e7f8a-0b1c-4d2e-3f4a-5b6c7d8e9f0a', 'Luna Dock', 'Mars Relay', '[]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('0e7f8a9b-1c2d-4e3f-4a5b-6c7d8e9f0a1b', 'Europa Port', 'Titan Anchorage', '[{"Name":"Aurora Gate"}]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('1f8a9b0c-2d3e-4f4a-5b6c-7d8e9f0a1b2c', 'Ceres Hub', 'Vesta Ring', '[{"Name":"Quasar Ridge"},{"Name":"Photon Belt"}]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('2a9b0c1d-3e4f-4a5b-6c7d-8e9f0a1b2c3d', 'Orion Outpost', 'Kepler Station', '[{"Name":"Ion Reef"},{"Name":"Nebula Drift"},{"Name":"Graviton Step"}]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('3b0c1d2e-4f5a-4b6c-7d8e-9f0a1b2c3d4e', 'Vega Spire', 'Polaris Gate', '[{"Name":"Starlight Sluice"},{"Name":"Comet Trail"},{"Name":"Void Crossing"},{"Name":"Solar Wake"}]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('4c1d2e3f-5a6b-4c7d-8e9f-0a1b2c3d4e5f', 'Andromeda Waystation', 'Nebula Junction', '[{"Name":"Darkmatter Node"},{"Name":"Echo Lattice"},{"Name":"Plasma Run"},{"Name":"Dust Halo"},{"Name":"Zenith Spur"}]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('5d2e3f4a-6b7c-4d8e-9f0a-1b2c3d4e5f6a', 'Helios Terminal', 'Nova Haven', '[]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('6e3f4a5b-7c8d-4e9f-0a1b-2c3d4e5f6a7b', 'Sirius Array', 'Astra Pier', '[{"Name":"Photon Belt"},{"Name":"Ion Reef"}]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('7f4a5b6c-8d9e-4f0a-1b2c-3d4e5f6a7b8c', 'Pulsar Bend', 'Drift Colony', '[{"Name":"Aurora Gate"},{"Name":"Nebula Drift"},{"Name":"Solar Wake"}]');
                                   INSERT OR IGNORE INTO Routes (Id, Origin, Destination, Checkpoints) VALUES ('8a5b6c7d-9e0f-4a1b-2c3d-4e5f6a7b8c9d', 'Ganymede Yard', 'Io Spindle', '[{"Name":"Quasar Ridge"},{"Name":"Graviton Step"},{"Name":"Comet Trail"},{"Name":"Dust Halo"}]');
                                   """;
}
