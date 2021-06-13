using Stratis.SmartContracts;

public class Ballot : SmartContract
{
    public Ballot(ISmartContractState smartContractState, byte[] proposalNames)
    : base(smartContractState)
    {
        ChairPerson = Message.Sender;
        var names = Serializer.ToArray<Proposal>(proposalNames);

        ValidateProposalNamesAndAssign(names);
    }
    public Address ChairPerson
    {
        get => PersistentState.GetAddress(nameof(ChairPerson));
        private set => PersistentState.SetAddress(nameof(ChairPerson), value);
    }
    public Proposal[] Proposals
    {
        get => PersistentState.GetArray<Proposal>(nameof(Proposals));
        private set => PersistentState.SetArray(nameof(Proposals), value);
    }
    private void ValidateProposalNamesAndAssign(Proposal[] proposals)
    {
        Assert(proposals.Length > 1, "Please provide at least 2 proposals");

        //TODO: check if proposal is not null or empty;

        this.Proposals = proposals;
    }
    private Voter GetVoter(Address address) => PersistentState.GetStruct<Voter>($"voter:{address}");
    private void SetVoter(Address address, Voter voter) => PersistentState.SetStruct($"voter:{address}", voter);
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
    public bool Vote(uint proposalId)
    {
        var voter = this.GetVoter(Message.Sender);

        //TODO: check if proposal id is not present

        Assert(voter.Weight == 1, "Has no right to vote.");

        Assert(!voter.Voted, "Already voted.");

        voter.Voted = true;
        voter.VoteProposalIndex = proposalId;

        Proposals[proposalId].VoteCount += voter.Weight;

        //TODO: add event

        return true;
    }
    public uint WinningProposal()
    {
        uint winningVoteCount = 0;
        uint winningProposalId = 0;

        for (uint i = 0; i < Proposals.Length; i++)
        {
            if(Proposals[i].VoteCount > winningVoteCount)
            {
                winningVoteCount = Proposals[i].VoteCount;
                winningProposalId = i;
            }
        }

        return winningProposalId;
    }
    public string WinnerName()
    {
        var winningProposalId = WinningProposal();
        var proposals = Proposals[winningProposalId];
        return proposals.Name;
    }
    public struct Proposal
    {
        public string Name;
        public uint VoteCount;        
    }
    public struct Voter
    {
        public uint Weight;
        public bool Voted;
        public uint VoteProposalIndex;
    }
    struct ElectionResult
    {
        [Index]
        public uint CandidateId;
        public string CandidateName;
        public uint VoteCount;
    }
}