using Stratis.SmartContracts;

public class Ballot : SmartContract
{
    public Ballot(ISmartContractState smartContractState)
    : base(smartContractState)
    {
        ChairPerson = Message.Sender;
    }

    public Address ChairPerson
    {
        get => PersistentState.GetAddress(nameof(ChairPerson));
        private set => PersistentState.SetAddress(nameof(ChairPerson), value);
    }

    public uint[] AllCandidates
    {
        get => PersistentState.GetArray<uint>(nameof(AllCandidates));
        private set => PersistentState.SetArray(nameof(AllCandidates), value);
    }

    private void SetCandidateName(uint candidateId, string candidateName)
    {
        PersistentState.SetString($"{candidateId}", candidateName);

        if (AllCandidates.Length > 0)
        {
            AllCandidates[AllCandidates.Length - 1] = candidateId;
        }
        else
        {           
            AllCandidates[0] = candidateId;
        }
    }

    private string GetCandidateName(uint candidateId)
    {
        return PersistentState.GetString($"{candidateId}");
    }

    private void SetCandidateVoteCount(uint candidateId, uint voteCount)
    {
        PersistentState.SetUInt32($"{candidateId}", voteCount);
    }

    private uint GetCandidateVoteCount(uint candidateId)
    {
        return PersistentState.GetUInt32($"{candidateId}");
    }

    private void SetVote(Address voter, bool vote)
    {
        PersistentState.SetBool($"{voter}:vote", vote);
    }

    private bool IsVoted(Address voter)
    {
        return PersistentState.GetBool($"{voter}:vote");
    }

    private void ProvideVotingRight(Address voterAddress)
    {
        PersistentState.SetBool($"{voterAddress}", true);
    }

    private bool CheckVotingRight(Address voterAddress)
    {
        return PersistentState.GetBool($"{voterAddress}");
    }

    public bool EnrollCandidate(uint candidateId, string candidateName)
    {
        Assert(Message.Sender == ChairPerson, "Only chairperson can enroll candidates.");

        Assert(string.IsNullOrEmpty(GetCandidateName(candidateId)), "candidate is already enrolled.");

        SetCandidateName(candidateId, candidateName);

        SetCandidateVoteCount(candidateId, 0);

        return true;
    }

    public bool GiveRightToVote(Address voter)
    {
        Assert(Message.Sender == ChairPerson, "Only chairperson can give right to vote.");

        Assert(!CheckVotingRight(voter), "The voter already have voting rights.");

        Assert(!IsVoted(Message.Sender), "Already voted.");

        ProvideVotingRight(voter);

        return true;

    }

    public void Vote(uint candidateId)
    {
        Assert(string.IsNullOrEmpty(GetCandidateName(candidateId)), "candidate id is not present.");

        Assert(!CheckVotingRight(Message.Sender), "Has no right to vote.");

        Assert(IsVoted(Message.Sender), "Already voted.");

        var totalVotesForCandidate = GetCandidateVoteCount(candidateId);
        SetCandidateVoteCount(candidateId, totalVotesForCandidate + 1);

        SetVote(Message.Sender, true);
    }

    public void End()
    {
        Assert(Message.Sender == ChairPerson, "Only chairperson can end the election.");

        for (uint i = 0; i < AllCandidates.Length; i++)
        {
            var name = GetCandidateName(AllCandidates[i]);
            var voteCount = GetCandidateVoteCount(AllCandidates[i]);

            Log(new ElectionResult
            {
                CandidateId = i,
                CandidateName = name,
                VoteCount = voteCount
            });
        }

        //TODO: end election
    }

    struct ElectionResult
    {
        [Index]
        public uint CandidateId;
        public string CandidateName;
        public uint VoteCount;
    }
}