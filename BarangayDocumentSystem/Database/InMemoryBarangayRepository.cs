// =====================================================================
//  PART:    Database - the in-memory store (demo mode and the rule checks)
//  ORIGIN:  leader_draft - Clint Wood Gado
//  EDITS:   Clint Wood Gado - v3.2: now a thin subclass of RepositoryBase;
//           the queries and the sample data moved out (see RepositoryBase
//           and SampleData) so they exist once, not once per storage
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Database;

/// <summary>
/// The repository with no database behind it.
///
/// Two things use it. My RuleChecks harness, which needs the rules and the
/// sample residents without a MySQL server on the build machine, and demo
/// mode (Storage=Memory in App.config) for a laptop where XAMPP is not
/// installed. Nothing survives closing the program, and the status bar says
/// so - the MySQL store is the real one.
///
/// Notice how little is left in this file. The working set, the searches,
/// the statistics and the rule that every request is priced on the way in
/// all live in RepositoryBase. All this class adds is "ids come from a
/// counter" - which is exactly the only thing that differs from MySQL,
/// where ids come from AUTO_INCREMENT.
/// </summary>
public sealed class InMemoryBarangayRepository : RepositoryBase
{
    private int _nextResidentId = 1;
    private int _nextRequestId = 1;

    /// <param name="fees">The fee schedule to price requests with.</param>
    /// <param name="seed">Load my sample residents and requests. True by
    /// default because both callers want them; the parameter exists so a
    /// test can start from an empty store.</param>
    public InMemoryBarangayRepository(FeeSchedule fees, bool seed = true) : base(fees)
    {
        if (seed) SampleData.Seed(this);
    }

    public override string StorageDescription => "In-memory demo \u2014 nothing is saved";

    // Storage here is the working set itself, so the hooks only have to
    // hand out ids. The base class does the rest.

    protected override int InsertResident(ResidentDetails details) => _nextResidentId++;

    protected override void UpdateResidentRow(Resident resident, ResidentDetails details) { }

    protected override void DeleteResidentRow(Resident resident) { }

    protected override int InsertRequest(
        Resident resident, DocumentType type, string purpose, RequestInput input,
        FeeAssessment assessment, DateTime filedOn) => _nextRequestId++;

    /// <summary>
    /// Nothing to write: the object the screen changed IS the stored row.
    /// I still get called, so the screens can call SaveRequest without
    /// knowing which store is underneath.
    /// </summary>
    protected override void UpdateRequestRow(DocumentRequest request) { }
}
