namespace VotingContract.Tests
{
    using Moq;
    using NBitcoin;
    using Stratis.SmartContracts;
    using Stratis.SmartContracts.CLR;
    using Stratis.SmartContracts.CLR.Serialization;
    using Xunit;
    using Proposal = Ballot.Proposal;
    using Voter = Ballot.Voter;

    public class BallotTests
    {
        private readonly Mock<ISmartContractState> mockContractState;
        private readonly Mock<IPersistentState> mockPersistentState;
        private readonly Mock<IInternalTransactionExecutor> mockInternalExecutor;
        private readonly Mock<IContractLogger> mockContractLogger;
        private readonly Mock<IBlock> mockBlock;
        private Serializer serializer;
        private Mock<Network> network;

        private Address contract;
        private Address chairPerson;
        private Address voter1;
        private Address voter2;
        private Address voter3;

        public BallotTests()
        {
            this.mockContractLogger = new Mock<IContractLogger>();
            this.mockPersistentState = new Mock<IPersistentState>();
            this.mockInternalExecutor = new Mock<IInternalTransactionExecutor>();
            this.mockContractState = new Mock<ISmartContractState>();

            this.network = new Mock<Network>();
            this.mockBlock = new Mock<IBlock>();

            this.mockContractState.Setup(s => s.Block).Returns(this.mockBlock.Object);
            this.mockContractState.Setup(s => s.PersistentState).Returns(this.mockPersistentState.Object);
            this.mockContractState.Setup(s => s.ContractLogger).Returns(this.mockContractLogger.Object);
            this.mockContractState.Setup(s => s.InternalTransactionExecutor).Returns(this.mockInternalExecutor.Object);

            this.serializer = new Serializer(new ContractPrimitiveSerializer(this.network.Object));
            this.mockContractState.Setup(s => s.Serializer).Returns(this.serializer);

            this.contract = "0x0000000000000000000000000000000000000001".HexToAddress();
            this.chairPerson = "0x0000000000000000000000000000000000000002".HexToAddress();
            this.voter1 = "0x0000000000000000000000000000000000000010".HexToAddress();
            this.voter2 = "0x0000000000000000000000000000000000000020".HexToAddress();
            this.voter3 = "0x0000000000000000000000000000000000000030".HexToAddress();
        }

        private (Ballot, Proposal[]) CreateContract()
        {
            var proposals = new[]
            {
                new Proposal { Name = "Joe Biden",  VoteCount = 0 },
                new Proposal { Name = "Donald Trump",  VoteCount = 0 }
            };

            this.mockContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.chairPerson, 0));
            this.mockPersistentState.Setup(s => s.GetArray<Proposal>(nameof(Ballot.Proposals))).Returns(proposals);

            var contract = new Ballot(this.mockContractState.Object, this.serializer.Serialize(proposals));
            return (contract, proposals);
        }

        private Voter NewVoter()
        {
            return new Voter()
            {
                Voted = false,
                Weight = 1
            };
        }

        [Fact]
        public void Constructor_ValidProposals_Success()
        {
            var (contract, proposals) = this.CreateContract();

            Assert.Equal(proposals, contract.Proposals);
            this.mockPersistentState.Verify(s => s.SetArray(nameof(Ballot.Proposals), proposals), Times.Once);
        }

        [Fact]
        public void Constructor_Fails_If_No_Proposals()
        {
            var proposal = new Proposal[] { };

            this.mockContractState.Setup(m => m.Message).Returns(new Message(this.contract, this.chairPerson, 0));
            this.mockPersistentState.Setup(s => s.GetArray<Proposal>(nameof(Ballot.Proposals))).Returns(proposal);

            Assert.ThrowsAny<SmartContractAssertException>(() => new Ballot(this.mockContractState.Object, this.serializer.Serialize(proposal)));
        }

        [Fact]
        public void GiveRightToVote_Success()
        {
            this.mockPersistentState.Setup(s => s.GetAddress(nameof(Ballot.ChairPerson))).Returns(this.chairPerson);
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter1}")).Returns(default(Voter));

            var (contract, proposals) = this.CreateContract();
            var result = contract.GiveRightToVote(this.voter1);

            Assert.True(result);
        }

        [Fact]
        public void GiveRightToVote_Fails_If_Sender_Is_Not_Chairperson()
        {
            this.mockPersistentState.Setup(s => s.GetAddress(nameof(Ballot.ChairPerson))).Returns(this.voter1);
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter1}")).Returns(default(Voter));

            var (contract, proposals) = this.CreateContract();

            Assert.ThrowsAny<SmartContractAssertException>(() => contract.GiveRightToVote(this.voter1));
            this.mockPersistentState.Verify(s => s.SetStruct($"voter:{this.voter1}", this.NewVoter()), Times.Never);

        }

        [Fact]
        public void GiveRightToVote_Fails_If_Voter_Has_Already_Right()
        {
            this.mockPersistentState.Setup(s => s.GetAddress(nameof(Ballot.ChairPerson))).Returns(this.chairPerson);
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter1}")).Returns(this.NewVoter());

            var (contract, proposals) = this.CreateContract();

            Assert.ThrowsAny<SmartContractAssertException>(() => contract.GiveRightToVote(this.voter1));
            this.mockPersistentState.Verify(s => s.SetStruct($"voter:{this.voter1}", this.NewVoter()), Times.Never);
        }

        [Fact]
        public void GiveRightToVote_Fails_If_Voter_Already_Voted()
        {
            var newVoter = this.NewVoter();
            newVoter.Weight = 0;
            newVoter.Voted = true;

            this.mockPersistentState.Setup(s => s.GetAddress(nameof(Ballot.ChairPerson))).Returns(this.chairPerson);
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter1}")).Returns(newVoter);

            var (contract, proposals) = this.CreateContract();

            Assert.ThrowsAny<SmartContractAssertException>(() => contract.GiveRightToVote(this.voter1));
            this.mockPersistentState.Verify(s => s.SetStruct($"voter:{this.voter1}", this.NewVoter()), Times.Never);
        }

        [Fact]
        public void Vote_Success()
        {
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter1}")).Returns(this.NewVoter());

            var (contract, proposals) = this.CreateContract();

            this.mockContractState.Setup(s => s.Message.Sender).Returns(this.voter1);

            var result = contract.Vote(0);

            Assert.True(result);
        }

        [Fact]
        public void WinningProposal_Success()
        {
            uint expectedWinningProposalIndex = 0;

            var voter1 = this.NewVoter();
            voter1.Voted = true;
            voter1.VoteProposalIndex = 0;

            var voter2 = this.NewVoter();
            voter2.Voted = true;
            voter2.VoteProposalIndex = 0;

            var voter3 = this.NewVoter();
            voter3.Voted = true;
            voter3.VoteProposalIndex = 1;

            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter1}")).Returns(voter1);
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter2}")).Returns(voter2);
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter3}")).Returns(voter3);

            var (contract, proposals) = this.CreateContract();

            var winnerProposalId = contract.WinningProposal();

            Assert.Equal(winnerProposalId, expectedWinningProposalIndex);
        }

        [Fact]
        public void WinnerName_Success()
        {
            uint expectedWinningProposalIndex = 0;
            string expectedWinnerProposalName = "Joe Biden";

            var voter1 = this.NewVoter();
            voter1.Voted = true;
            voter1.VoteProposalIndex = 0;

            var voter2 = this.NewVoter();
            voter2.Voted = true;
            voter2.VoteProposalIndex = 0;

            var voter3 = this.NewVoter();
            voter3.Voted = true;
            voter3.VoteProposalIndex = 1;

            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter1}")).Returns(voter1);
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter2}")).Returns(voter2);
            this.mockPersistentState.Setup(s => s.GetStruct<Voter>($"voter:{this.voter3}")).Returns(voter3);

            var (contract, proposals) = this.CreateContract();

            var winnerProposalId = contract.WinningProposal();

            var winnerProposalName = contract.WinnerName();

            Assert.NotNull(winnerProposalName);
            Assert.Equal(expectedWinnerProposalName, proposals[expectedWinningProposalIndex].Name);
        }
    }
}
