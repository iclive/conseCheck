## Hotstuff

### Algorithm: Next Round

```pseudo
Function NextRound()
    If round < R
        setNextProposer:
        round ← round + 1
        leader ← (leader + 1) mod N
        leaf ← round × 10

        prevote0.Clear(), prevote1.Clear(), prevote2.Clear(), prevote3.Clear()
        precommitVote0.Clear(), precommitVote1.Clear(), precommitVote2.Clear(), precommitVote3.Clear()
        commitVote0.Clear(), commitVote1.Clear(), commitVote2.Clear(), commitVote3.Clear()
            // Clear previous round voting information

        BlockChain()
    Else
        Skip
    EndIf
EndFunction
```

### Algorithm: Prepare Leader

```pseudo
Function PrepareLeader(i)
    If leader = i
        prepareLeader.i:
        parentHash ← leaf
            // Get the hash of the current leaf as parent reference
        proposedBlock ← new Block(round × 10, parentHash)
            // Create a new block proposal for the current round
        proposalList.Set(i, new Proposal(proposedBlock, new Signature(i)))
            // Store the proposal by leader's signature
        do:
            [i = 0] ( c01!proposalList.Get(i) → Skip ∥ 
                      c02!proposalList.Get(i) → Skip ∥ 
                      c03!proposalList.Get(i) → Skip )
                // Leader 0 broadcasts the proposal
            [i = 1] (etc...)
        → Skip
    ElsIf i = 0 ∧ i ≠ leader
        If leader = 1
            c10?y → { proposalList.Set(i, y) } → Skip
                // Replica 0 receives the proposal from leader 1 and stores it
        ElsIf leader = 2
            c20?y → { proposalList.Set(i, y) } → Skip
        ElsIf leader = 3
            c30?y → { proposalList.Set(i, y) } → Skip
        EndIf
    Else
        etc...
    EndIf
EndFunction
```

### Algorithm: Prepare Replica

```pseudo
Function PrepareRep(i)
    If i == 0
        prepareVote:
        Call Prevote0()
            // Replica 0 casts a prevote based on received proposal

        If leader == 0
            If voteCounter == 3
                voteCounter = 0 → Skip
                    // Process votes once quorum threshold is met
            ElsIf leader == 1
                c01!tmpVote0 → Skip
                    // Send vote to leader 1
            ElsIf leader == 2
                c02!tmpVote0 → Skip
            ElsIf leader == 3
                c03!tmpVote0 → Skip
            EndIf
        EndIf
    Else
        /* etc... */
    EndIf
EndFunction
```

### Algorithm: Precommit Leader

```pseudo
Function PrecommitLeader(i)
    If leader = i
        precommitLeader.i:
        If i = 0
            tmpProposedBlock ← prevote0.FindMinSupportBlock(MAJORITY)
                // Select the first block with majority prevote support

            proposalList.Set(i, new Proposal(
                tmpProposedBlock,
                new QC(0, tmpProposedBlock.GetHash(), prevote0)
            ))
                // Generate Quorum Certificate (QC) and store the proposal
        EndIf

        (etc...)

        do:
            [i = 0] (
                c01!proposalList.Get(i) → Skip ∥
                c02!proposalList.Get(i) → Skip ∥
                c03!proposalList.Get(i) → Skip
            )
                // Leader 0 broadcasts QC-wrapped proposal to other replicas

            [i = 1] (
                c10!proposalList.Get(i) → Skip ∥
                c12!proposalList.Get(i) → Skip ∥
                c13!proposalList.Get(i) → Skip
            )
        → Skip

    ElseIf i = 0 ∧ i ≠ leader
        If leader = 1
            c10?y → { proposalList.Set(i, y) } → Skip
                // Replica 0 receives precommit proposal from leader 1
        ElseIf leader = 2
            (etc...)
        EndIf
    Else
        (etc...)
    EndIf
EndFunction
```

### Algorithm: Precommit Replica

```pseudo
Function PrecommitRep(i)
    If i == 0
        precommitVote:
        Call Precommit0()
            // Replica 0 casts precommit vote

        If leader == 0
            If voteCounter == 3
                voteCounter = 0 → Skip
                    // Process votes once quorum threshold met
            ElseIf leader == 1
                c01!tmpVote0 → Skip
                    // Send precommit vote to leader 1
            ElseIf leader == 2
                (etc...)
            EndIf
        EndIf
    Else
        /* etc... */
    EndIf
EndFunction
```

### Algorithm: Commit Leader

```pseudo
Function CommitLeader(i)
    If leader = i
        precommitPost:
        If i = 0
            tmpProposedBlock ← precommitVote0.FindMinSupportBlock(MAJORITY)
            proposalList.Set(i, new Proposal(
                tmpProposedBlock,
                new QC(1, tmpProposedBlock.GetHash(), precommitVote0)
            ))
                // Create commit proposal with QC from precommit votes
        EndIf

        (etc...)

        do:
            [i = 0] (c01!proposalList.Get(i) → Skip ∥
                     c02!proposalList.Get(i) → Skip ∥
                     c03!proposalList.Get(i) → Skip)
            [i = 1] (c10!proposalList.Get(i) → Skip ∥
                     c12!proposalList.Get(i) → Skip ∥
                     c13!proposalList.Get(i) → Skip)
        → Skip

    ElseIf i = 0 ∧ i ≠ leader
        If leader = 1
            c10?y → { proposalList.Set(i, y) } → Skip
        ElseIf leader = 2
            (etc...)
        EndIf
    Else
        (etc...)
    EndIf
EndFunction
```

### Algorithm: Commit Replica

```pseudo
Function CommitRep(i)
    If i == 0
        commitVote:
        Call Commit0()
        
        If leader == 0
            If voteCounter == 3
                voteCounter = 0 → Skip
                // Process votes once quorum threshold is met
            ElseIf leader == 1
                c01!tmpVote0 → Skip
                // Send commit vote to leader 1
            ElseIf leader == 2
                (etc...)
            EndIf
        EndIf
    Else
        /* etc... */
    EndIf
EndFunction
```

### Algorithm: Decide Leader

```pseudo
Function DecideLeader(i)
    If leader == i
        precommitPost:
        If i == 0
            tmpProposedBlock ← precommitVote0.FindMinSupportBlock(MAJORITY)
            proposalList.Set(i, new Proposal(
                tmpProposedBlock,
                new QC(2, tmpProposedBlock.GetHash(), commitVote0)
            ))
            // Create decide proposal with commit QC
        EndIf

        (etc...)

        do:
            [i = 0] (
                c01!proposalList.Get(i) → Skip ∥
                c02!proposalList.Get(i) → Skip ∥
                c03!proposalList.Get(i) → Skip
            )
            (etc...)
        → Skip

    ElseIf i == 0 ∧ i ≠ leader
        If leader == 1
            c10?y → { proposalList.Set(i, y) } → Skip
        ElseIf leader == 2
            c20?y → { proposalList.Set(i, y) } → Skip
        ElseIf leader == 3
            c30?y → { proposalList.Set(i, y) } → Skip
        EndIf

    Else
        (etc...)
    EndIf
EndFunction
```

### Algorithm: Decide Replica

```pseudo
Function DecideRep(i)
    If i == 0
        addtoChain.i:
        tmpProposal0 ← proposalList.Get(0)
        tmpProposedBlock0 ← tmpProposal0.GetBlock()
        tmpQC0 ← tmpProposal0.GetQC()

        If tmpProposedBlock0.GetHash() ≠ -1 
           ∧ tmpQC0.GetBlockHash() == tmpProposedBlock0.GetHash()
            chain0.Add(tmpProposedBlock0)
            // Only commit if QC matches block hash
        EndIf

        → Skip
    Else
        (etc...)
    EndIf
EndFunction
