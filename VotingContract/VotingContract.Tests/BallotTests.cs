namespace VotingContract.Tests
{
    using Moq;
    using Stratis.SmartContracts;
    using Stratis.SmartContracts.CLR;
    using Xunit;
    using static Ballot;

    public class BallotTests
    {
        private const string ChairPerson = "0x0000000000000000000000000000000000000001";
        private const uint CandidateOneId = 1;
        private const uint CandidateTwoId = 2;
        private const string CandiateOneName = "John";
        private const string CandiateTwoName = "Trump";
        private const string Voter1 = "0x0000000000000000000000000000000000000010";
        private const string Voter2 = "0x0000000000000000000000000000000000000020";
        private const string Voter3 = "0x0000000000000000000000000000000000000030";
        private const string Voter4 = "0x0000000000000000000000000000000000000040";
        private const string Voter5 = "0x0000000000000000000000000000000000000050";

        private static readonly Address ChairPersonAddress = ChairPerson.HexToAddress();
        private static readonly Address Voter1Address = Voter1.HexToAddress();
        private static readonly Address Voter2Address = Voter2.HexToAddress();
        private static readonly Address Voter3Address = Voter3.HexToAddress();
        private static readonly Address Voter4Address = Voter4.HexToAddress();
        private static readonly Address Voter5Address = Voter5.HexToAddress();


        private readonly Mock<ISmartContractState> mockContractState;
        private readonly Mock<IPersistentState> mockPersistentState;
        private readonly Mock<IInternalTransactionExecutor> mockInternalExecutor;
        private readonly Mock<IContractLogger> mockContractLogger;

        public BallotTests()
        {
            this.mockContractLogger = new Mock<IContractLogger>();
            this.mockPersistentState = new Mock<IPersistentState>();
            this.mockInternalExecutor = new Mock<IInternalTransactionExecutor>();
            this.mockContractState = new Mock<ISmartContractState>();
            this.mockContractState.Setup(s => s.PersistentState).Returns(this.mockPersistentState.Object);

            this.mockContractState.Setup(s => s.ContractLogger).Returns(this.mockContractLogger.Object);
            this.mockContractState.Setup(s => s.InternalTransactionExecutor).Returns(this.mockInternalExecutor.Object);
        }

        private Ballot NewBallot()
        {
            mockContractState.Setup(x => x.Message.Sender).Returns(ChairPersonAddress);
            return new Ballot(this.mockContractState.Object);
        }

        [Fact]
        public void GiveRightToVote_Succeeds()
        {            
            this.mockPersistentState.Setup(s => s.GetAddress("ChairPerson")).Returns(ChairPersonAddress);
            this.mockPersistentState.Setup(s => s.GetBool($"{Voter1Address}")).Returns(false);

            var contract = this.NewBallot();
            var result = contract.GiveRightToVote(Voter1Address);

            this.mockPersistentState.Verify(s => s.SetBool($"{Voter1Address}", true), Times.Once);
            Assert.True(result);
        }

        [Theory]
        [InlineData(CandidateOneId, CandiateOneName)]
        public void EnrollCandidate(uint candidateId, string candidateName)
        {
            this.mockPersistentState.Setup(s => s.GetAddress("ChairPerson")).Returns(ChairPersonAddress);
            this.mockPersistentState.Setup(s => s.GetString($"{candidateId}")).Returns(string.Empty);
            this.mockPersistentState.Setup(s => s.GetArray<uint>("AllCandidates")).Returns(new uint[0]);

            var contract = this.NewBallot();
            var result = contract.EnrollCandidate(candidateId, candidateName);
            
            this.mockPersistentState.Verify(s => s.SetString($"{candidateId}", $"{candidateName}"), Times.Once);
            Assert.True(result);
        }
    }
}
