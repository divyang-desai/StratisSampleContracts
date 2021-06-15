using Stratis.SmartContracts;

/// <summary>
/// Implements voting process
/// </summary>

[Deploy]
public class Ballot : SmartContract
{
    /// <summary>
    /// Constructor to create new ballot contract.
    /// </summary>
    /// <param name="smartContractState">The smart contract state.</param>
    /// <param name="proposalNames">List of proposals.</param>
    public Ballot(ISmartContractState smartContractState, byte[] proposalNames)
    : base(smartContractState)
    {
        ChairPerson = Message.Sender;
        var names = Serializer.ToArray<Proposal>(proposalNames);

        ValidateProposalNamesAndAssign(names);
    }

    /// <summary>
    /// The contract owner wallet address.
    /// </summary>
    public Address ChairPerson
    {
        get => PersistentState.GetAddress(nameof(ChairPerson));
        private set => PersistentState.SetAddress(nameof(ChairPerson), value);
    }

    /// <summary>
    /// List of proposals.
    /// </summary>
    public Proposal[] Proposals
    {
        get => PersistentState.GetArray<Proposal>(nameof(Proposals));
        private set => PersistentState.SetArray(nameof(Proposals), value);
    }
    
    /// <summary>
    /// Validation for the list of proposals pass during the contract deployment.
    /// </summary>
    /// <param name="proposals"></param>
    private void ValidateProposalNamesAndAssign(Proposal[] proposals)
    {
        Assert(proposals.Length > 1, "Please provide at least 2 proposals");
        this.Proposals = proposals;
    }

    /// <summary>
    /// Gets voter detail.
    /// </summary>
    /// <param name="address">The voter wallet address.</param>
    /// <returns>Voter struct</returns>
    private Voter GetVoter(Address address) => PersistentState.GetStruct<Voter>($"voter:{address}");

    /// <summary>
    /// Sets the struct of voter detail.
    /// </summary>
    /// <param name="address">The voter wallet address.</param>
    /// <param name="voter">Voter struct</param>
    private void SetVoter(Address address, Voter voter) => PersistentState.SetStruct($"voter:{address}", voter);

    /// <summary>
    /// Provide voting right to a voter to vote on this ballot.
    /// </summary>
    /// <param name="voterAddress">The voter wallet address.</param>
    /// <returns>Boolean flag value.</returns>
    public bool GiveRightToVote(Address voterAddress)
    {
        Assert(Message.Sender == ChairPerson, "Only chairperson can give right to vote.");

        var voter = this.GetVoter(voterAddress);

        Assert(voter.Weight == 0, "The voter already have voting rights.");

        Assert(!voter.Voted, "Already voted.");

        voter.Weight = 1;

        this.SetVoter(voterAddress, voter);

        return true;
    }

    /// <summary>
    ///  Give your vote to proposal.
    /// </summary>
    /// <param name="proposalId">Proposal index of proposal in the proposals array.</param>
    /// <returns>Boolean flag value.</returns>
    public bool Vote(uint proposalId)
    {
        var voter = this.GetVoter(Message.Sender);

        Assert(voter.Weight == 1, "Has no right to vote.");

        Assert(!voter.Voted, "Already voted.");

        voter.Voted = true;
        voter.VoteProposalIndex = proposalId;

        Proposals[proposalId].VoteCount += voter.Weight;

        this.SetVoter(Message.Sender, voter);
        
        Log(new Voter
        {
            Voted = true,
            Weight = 1,
            VoteProposalIndex = proposalId
        });

        return true;
    }

    /// <summary>
    /// Computes the winning proposal taking all previous votes into account.
    /// </summary>
    /// <returns>Id(index) of the winning proposal.</returns>
    public uint WinningProposal()
    {
        uint winningVoteCount = 0;
        uint winningProposalId = 0;

        for (uint i = 0; i < Proposals.Length; i++)
        {
            if (Proposals[i].VoteCount > winningVoteCount)
            {
                winningVoteCount = Proposals[i].VoteCount;
                winningProposalId = i;
            }
        }

        return winningProposalId;
    }
    
    /// <summary>
    /// Gets name of the winning proposal.
    /// </summary>
    /// <returns>Name of the winning proposal.</returns>
    public string WinnerName()
    {
        var winningProposalId = WinningProposal();
        var proposals = Proposals[winningProposalId];
        return proposals.Name;
    }

    public struct Proposal
    {
        /// <summary>
        /// Name of the proposal.
        /// </summary>
        public string Name;

        /// <summary>
        /// Total vote received for the proposal.
        /// </summary>
        public uint VoteCount;
    }
    public struct Voter
    {
        /// <summary>
        /// Voting weight of the voter.
        /// </summary>
        public uint Weight;

        /// <summary>
        /// Is the voter voted
        /// </summary>
        public bool Voted;

        /// <summary>
        /// Proposal index of the vote.
        /// </summary>
        public uint VoteProposalIndex;
    }
}