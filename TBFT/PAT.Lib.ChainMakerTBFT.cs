using System;
using System.Collections;
using System.Collections.Generic;
using PAT.Common.Classes.Expressions.ExpressionClass;

//the namespace must be PAT.Lib, the class and method names can be arbitrary
namespace PAT.Lib {

	public static class TbftConstants{
        public static readonly int TBFT_STEP_NewHeight = 0;
        public static readonly int TBFT_STEP_NewRound = 1;
        public static readonly int TBFT_STEP_Propose = 2;
        public static readonly int TBFT_STEP_Prevote = 3;
        public static readonly int TBFT_STEP_Prevote_Wait = 4;
        public static readonly int TBFT_STEP_Precommit = 5;
        public static readonly int TBFT_STEP_Precommit_Wait = 6;
        public static readonly int TBFT_STEP_Commit = 7;
    }


	// 共识节点列表，每个节点都是一个ConsensusNode对象
	public class ConsensusNodes : ExpressionValue{
		public Dictionary<int, ConsensusNode> nodes;
		
		public ConsensusNodes(){
			this.nodes = new Dictionary<int, ConsensusNode>();
		}
		
		public ConsensusNodes( Dictionary<int, ConsensusNode> nodes){
			this.nodes = nodes;
		}
		
		public void addConsensusNode(ConsensusNode node){
			lock(this.nodes){
				if (!this.nodes.ContainsKey(node.nodeId)){
					this.nodes[node.nodeId] = node;
				}
			}
		}
		
		public void clearNodes(){
			lock(this.nodes){
				this.nodes.Clear();
			}
		}
		
		public ConsensusNode getByKey(int nodeId){
			lock(this.nodes){
				return this.nodes[nodeId];
			}
		}

		public void startNode(int nodeId){
			lock(this.nodes){
				this.nodes[nodeId].start(1);
			}
		}

		public void addVote(int nodeId, int voteType, Vote vote) {
			var node = this.nodes[nodeId];
			switch (voteType){
				case 1:
					lock(node.preVotes){
						string roundKey = vote.height+"_"+vote.round;
						if (!node.preVotes.ContainsKey(roundKey)){
							node.preVotes[roundKey] = new Dictionary<int, Vote>();
							node.preVotes[roundKey][vote.voter] = vote;
						}else if(node.preVotes.ContainsKey(roundKey) 
							&& !node.preVotes[roundKey].ContainsKey(vote.voter)){
							node.preVotes[roundKey][vote.voter] = vote;
						}
					}
					break;
				case 2:
					lock(node.precommites){
						string roundKey = vote.height+"_"+vote.round;
						if (!node.precommites.ContainsKey(roundKey)){
							node.precommites[roundKey] = new Dictionary<int, Vote>();
							node.precommites[roundKey][vote.voter] = vote;
						}else if(node.precommites.ContainsKey(roundKey) 
							&& !node.precommites[roundKey].ContainsKey(vote.voter)){
							node.precommites[roundKey][vote.voter] = vote;
						}
					}
					break;
			}

			//return isMajority(voteType, 4, nodeId, nextStep);
		}

		public bool isMajority(int voteType, int total, int nodeId,int nextStep, int height, int round){
			string roundKey = height+"_"+round;
			var node = this.nodes[nodeId];
			int majority = total * 2 / 3 +1;
			switch (voteType){
				case 1:
					lock(node.preVotes){
						int cnt = 0;
						if (!node.preVotes.ContainsKey(roundKey)) {
							return false;
      					}

						foreach (var kv in node.preVotes[roundKey]) {
							if(node.proposal == null){
								if ( kv.Value.hash == 0) {
									cnt++;
								}
							}else{
								if (kv.Value.hash == node.proposal.block.blockHash) {
									cnt++;
								}
							}
						}
						if (cnt >= majority){
							return true;
						}
						return false;
					}
				case 2:
					lock(node.precommites){
						int cnt = 0;
						if (!node.precommites.ContainsKey(roundKey)) {
							return false;
      					}

						foreach (var kv in node.precommites[roundKey]) {
							if(node.proposal == null){
								if ( kv.Value.hash == 0) {
									cnt++;
								}
							}else{
								if (kv.Value.hash == node.proposal.block.blockHash) {
									cnt++;
								}
							}
						}
						if (cnt >= majority){
							return true;
						}
						return false;
					}
				default:
					return false;
			}
		}

		public int getNodeHeight(int nodeId){
			lock(this.nodes){
				return nodes[nodeId].height;
			}
		}

		public Block latestBlock(int nodeId) {
			var node = this.nodes[nodeId];
			lock(node.blocks) {
				Block block = null;
				if (node.blocks != null) {
					block = node.blocks[node.height];
				}
				return block;
			}
		}
		
		public int reachTargetHeight(int height){
			lock(this.nodes){
				if(this.nodes.Count > 0){
					int cnt = 0;
                
					foreach (var kv  in nodes) {
						if (kv.Value.height >= height) {
							cnt ++;
						}
					}
					if( cnt >= nodes.Count * 2 / 3 +1){
						return 1;
					}
					return 0;
				}else {
					return 0;
				}
			}
		}

		public int MajorityBlockHash( int height){
			lock(this.nodes){
				foreach (var cn in nodes) {
					int hash = cn.Value.getBlockHash(height);
					if(hash > 0) {
						return hash;
					}
				}
				return -1;
			}
		}

		public int hasTimeout(){
			lock(this.nodes){
				int cnt = 0;
				foreach (var cn in nodes) {
					//cnt += cn.Value.hasTimeout();
					return cn.Value.hasTimeout();
				}
				if( cnt > nodes.Count * 2 / 3 + 1) {
					return 1;
				}
				return 0;
			}
		}

		public int hasDifferentHash(){
			lock(this.nodes){
				int cnt = 0;
				foreach (var cn in nodes) {
						cnt += cn.Value.hasDifferentHash();
					}
					if( cnt > nodes.Count * 2 / 3 + 1) {
						return 1;
					}
					return 0;
			
				// if(this.nodes.Count > 0){
				// 	int cnt = 0;
				// 	foreach (var cn in nodes) {
				// 		cnt += cn.Value.hasDifferentHash();
				// 	}
				// 	if( cnt > nodes.Count * 2 / 3 + 1) {
				// 		return 1;
				// 	}
				// 	return 0;
				// }else{
				// 	return 0;
				// }
			}
		}

		/// Return a deep clone of the hash table
        /// NOTE: this must be a deep clone, shallow clone may lead to strange behaviors.
        /// This method must be overriden
        public override ExpressionValue GetClone() {
            return new ConsensusNodes(this.nodes);
        }

        /// Return the compact string representation of the hash table.
        /// This method must be overriden
        /// Smart implementation of this method can reduce the state space and speedup verification 
        public override string ExpressionID {
            get {
                string returnString = "\n";
                foreach (var cn in nodes) {
                    returnString += "\n" + cn.Value.ToString() + ",";
                }
                returnString += "";
                return returnString;
               }
        }
        
        /// Return the string representation of the hash table.
        /// This method must be overriden
        public override string ToString() {
            return "[" + ExpressionID + "]";
        }
	}

	// 共识节点数据结构，提供节点对数据的存储与处理
	public class ConsensusNode : ExpressionValue {
		public int	 			chainId;			// 联盟链ID
		public int	 			nodeId;				// 节点ID
		public int 				runningState;		// 模块运行状态： 0：未运行，1：运行中
		public int 				height;				// 区块高度
		public int              consensusHeight;    // 共识高度
		public int 				round;				// 共识轮次
		public int 				nodes;				// 共识节点数量
		public int              lastProposer;       // 上一个区块的提议者
		public int 				proposer;			// 最新区块的提议者
		public Dictionary<string,Dictionary<int,Vote>>       preVotes;			// 收集到的prevote投票
		public Dictionary<string,Dictionary<int,Vote>>		precommites;		// 收集到的precommit投票
		public Proposal			proposal;			// 当前共识的proposal
		public int 				step;				// 当前共识所处的步骤
		public Dictionary<int,Block> blocks;		// 区块缓存
		public Dictionary<int, Dictionary<int,int>> delayTimes;     // 节点在每个round的延时列表
		
		public ConsensusNode(){
			this.preVotes = new Dictionary<string, Dictionary<int, Vote>>();
			this.precommites = new Dictionary<string, Dictionary<int, Vote>>();
			this.blocks = new Dictionary<int, Block>();
			this.delayTimes = new Dictionary<int, Dictionary<int, int>>();
			this.step = -1;
		}
		
		public ConsensusNode(int chainId, int nodeId, int height, int round,  int nodes, int lastProposer, int proposer){
			this.chainId = chainId;
			this.nodeId = nodeId;
			this.height = height;
			this.consensusHeight = height;
			this.round  = round;
			this.nodes  = nodes;
			this.lastProposer = lastProposer;
			this.proposer = proposer;
			this.preVotes = new Dictionary<string, Dictionary<int, Vote>>();
			this.precommites = new Dictionary<string, Dictionary<int, Vote>>();
			this.blocks = new Dictionary<int, Block>();
			this.delayTimes = new Dictionary<int, Dictionary<int, int>>();
			this.step = -1;
		}

		public ConsensusNode(int chainId, int nodeId, int height,int cHeight, int round, int nodes,int lastProposer, int proposer, Dictionary<string,Dictionary<int, Vote>> prevotes, Dictionary<string,Dictionary<int, Vote>> precommit, 
		Proposal proposal, int step,Dictionary<int,Block> blocks, Dictionary<int, Dictionary<int, int>> delayTimes){
			this.chainId = chainId;
			this.nodeId = nodeId;
			this.height = height;
			this.consensusHeight = cHeight;
			this.round  = round;
			this.nodes  = nodes;
			this.proposer = proposer;
			this.lastProposer = lastProposer;
			this.preVotes = prevotes;
			this.precommites = precommit;
			this.proposal = proposal;
			this.step = step;
			this.blocks = blocks;
			this.delayTimes = delayTimes;
		}
		public void start(int x) {
            if (this.runningState != 1)
            {
                this.runningState = 1;
            }
        }

        public void stop(){
			this.runningState = 0;
		}

		public int getNodeId() {
			return this.nodeId;
		}

		public int getHeight(){
			return this.height;
		}
		
		public int getProposer() {
			return this.proposer;
		}

		public int setProposal(Proposal proposal) {

			//if(this.consensusHeight == proposal.getHeight() && this.round == proposal.getRound()){
				this.proposal = proposal;
				return 1;
			//}
			//return 0;
		}

		public void setDelayTime(int height, int round, int delayTime) {
			lock(this.delayTimes){
				if(!this.delayTimes.ContainsKey(height)){
					Dictionary<int, int> roundDelay = new Dictionary<int, int>();
					roundDelay[round] = delayTime;
					this.delayTimes[height] = roundDelay;
				}else{
					this.delayTimes[height][round] = delayTime;
				}
			}
		}

		public int getDelayTime(int height, int round) {
			lock(this.delayTimes) {
				if (this.delayTimes.ContainsKey(height)) {
					if (this.delayTimes[height].ContainsKey(round)) {
						return this.delayTimes[height][round];
					}
				}
				return 0;
			}
		}
		
		public void processBlock(Block block) {
			this.height = block.getHeight();
			this.consensusHeight = block.getHeight();
			this.lastProposer = block.getProposer();
			this.proposer = block.getProposer();
			// lock(this.preVotes) {
			// 	this.preVotes.Clear();
			// }
			// lock(this.precommites) {
			// 	this.precommites.Clear();
			// }
			this.blocks[block.getHeight()] = block;
			this.round = 0;
		}

		public void newHeight(int height) {
			if(this.consensusHeight >= height){
                return;
            }
			this.consensusHeight = height;
			this.round = 0;
		}

		public void newRound(int height, int round){
			//  if(this.consensusHeight > height 
            // || this.round > round 
            // || (this.round == round && this.step != TbftConstants.TBFT_STEP_NewHeight)){
            //     return;

            // }

			this.round = round;
			// lock(this.preVotes) {
			// 	this.preVotes.Clear();
			// }
			// lock(this.precommites) {
			// 	this.precommites.Clear();
			// }
			this.proposal = null;
		}
		
		public int isProposer(int height, int round){
			int proposerOffset = this.lastProposer;
			if (height % this.nodes == 0 ) {
				proposerOffset++;
			}
			int roundOffset = round % this.nodes;
			int currentProposer = (proposerOffset + roundOffset) % nodes;
			this.proposer = currentProposer;
			return currentProposer;
		}

		public int getLastProposer(){
			return this.lastProposer;
		}

		public int needGenProposal(int height, int round, bool disguiser){
			int proposer = this.isProposer(height, round);
			if(proposer == this.nodeId){
				return 1;
			}else if(disguiser && this.nodeId == (proposer + 1)%this.nodes){
				return 1;
			}
			return 0;
		}

		public int isDisguiser(int height, int round){
			int proposer = this.isProposer(height, round);
			if(this.nodeId == (proposer + 1)%this.nodes){
				return 1;
			}
			return 0;
		}

		public Proposal generateProposal() {
			List<int> qc = new List<int>();
			qc.Add(this.nodeId);
			
			int disguiser = 1;
			int proposer = this.isProposer(this.consensusHeight, this.round);
			if (this.nodeId == proposer){
				disguiser = 0;
			}
			// 正常的区块哈希
			int blockHash = 1;
			if (disguiser == 1) {
				blockHash = 2;
			}

			// int voter = this.nodeId;
			// if (disguiser == 1) {
			// 	voter = Utils.NextProposer( this.lastProposer, this.height, this.round, this.nodes);
			// }

			Block block = new Block(this.chainId,1,this.consensusHeight,blockHash,this.nodeId,1,1);
			Proposal proposal = new Proposal(this.consensusHeight,this.round,proposer, 1, qc,block );
			//this.proposal = proposal;

			return proposal;
		}

		public int noProposal(){
			if (this.proposal == null && this.step == TbftConstants.TBFT_STEP_Propose){
				return 1;
			}
			return 0;
		}

		public Vote generatePreVote(int flag) {
			var hash = 0;
			if (flag == 1 && this.proposal != null) {
				hash = this.proposal.block.blockHash;
			}
			Vote vote = new Vote(this.consensusHeight, this.round,2,this.nodeId,1,hash);
			
			return vote;	
		}

		public void savePrevote(Vote vote) {
			lock(this.preVotes) {
				string roundKey = vote.height+"_"+vote.round;
				if (!this.preVotes.ContainsKey(roundKey)){
					Dictionary<int, Vote> votes = new Dictionary<int, Vote>();
					votes[vote.voter] = vote;
					this.preVotes[roundKey] = votes;
				}else{
					this.preVotes[roundKey][vote.voter] = vote;
				}
			}
		}

		public int isMajorityPrevotes(){
			string roundKey = this.consensusHeight+"_"+this.round;
			int majority = this.nodes * 2 / 3 + 1;
			lock(this.preVotes) {
				if (this.preVotes.ContainsKey(roundKey)){
					//return 1;
					int validHash = 0;
					int nilHash = 0;
					int otherHash = 0;
					foreach(var kv in this.preVotes[roundKey]){
						if (kv.Value.hash == 0) {
							nilHash ++;
						}else if(kv.Value.hash == 1){
							validHash ++;
						}else{
							otherHash ++;
						}
					}
					if (validHash >= majority || nilHash >= majority || otherHash >= majority) {
						return 1;
					}
				}
			}
			return 0;
		}

		public Vote generatePreCommit(int flag) {
			var hash = 0;
			if (flag == 1 && this.proposal != null) {
				hash = this.proposal.block.blockHash;
			}
			Vote vote = new Vote(this.consensusHeight, this.round,3,this.nodeId,1, hash);
			return vote;	
		}

		public void savePrecommit( Vote vote) {
			lock(this.precommites) {
				string roundKey = vote.height+"_"+vote.round;
				if (!this.precommites.ContainsKey(roundKey)){
					Dictionary<int, Vote> votes = new Dictionary<int, Vote>();
					votes[vote.voter] = vote;
					this.precommites[roundKey] = votes;
				}else{
					this.precommites[roundKey][vote.voter] = vote;
				}
			}
		}

		public int isMajorityPrecommits(){
			string roundKey = this.consensusHeight+"_"+this.round;
			int majority = this.nodes * 2 / 3 + 1;
			lock(this.precommites) {
				if (this.precommites.ContainsKey(roundKey)){
					//return 1;
					int validHash = 0;
					int nilHash = 0;
					int otherHash = 0;
					foreach(var kv in this.precommites[roundKey]){
						if (kv.Value.hash == 0) {
							nilHash ++;
						}else if(kv.Value.hash == 1){
							validHash ++;
						}else{
							otherHash ++;
						}
					}
					if (validHash >= majority || nilHash >= majority || otherHash >= majority) {
						return 1;
					}
				}
			}
			return 0;
		}


		public void setStep(int step) {
			this.step = step;
		}

		public void commitBlock(){
			var block = this.proposal.block;
			this.height = block.blockHeight;
		}

		public void contactBlock(Block block) {
			lock(this.blocks) {
				this.height = block.blockHeight;
				this.blocks[block.blockHeight] = block;
			}
		}

		public Block getProposalBlock(){
			return this.proposal.block;
		}

		public int reachTarget(int target){
			if (this.height >= target) {
				return 1;
			}
			return 0;
		}

		public int getBlockHash(int height){
			lock(this.blocks) {
				if (this.blocks.ContainsKey(height)) {
					return this.blocks[height].blockHash;
				}
			}
			return 0;
		}

		public int hasTimeout(){
			if (this.round > 0 ){
				return 1;
			}
			return 0;
		}

		public void contactNewBlock(int chainId, int blockType, int blockHeight, int blockHash, int proposer, int sign, long blocktime) {

			lock(this.blocks) {
				Block block = new Block(chainId,blockType,blockHeight,blockHash,proposer,sign,blocktime);
				this.height = blockHeight;
				this.blocks[blockHeight] = block;
			}
		}

		public Block latestBlock() {
			lock(this.blocks) {
				Block block = null;
				if (this.blocks != null) {
					block = this.blocks[this.height];
				}
				return block;
			}
		}

		public int isNilBlock(int step){
			if(this.proposal == null){
				return 1;
			}
			return 0;
		}

		public int isNilConsensus(int height, int round, int step){
			string roundKey = height+"_"+round;
			switch( step){
				case 5: 
				 	lock(this.preVotes){
						if (this.preVotes.ContainsKey(roundKey)){
							int cnt = 0;
							foreach (var item in this.preVotes[roundKey]){
								if (item.Value.hash == 0){
									cnt ++;
								}
							}
							if(cnt >= this.nodes * 2 / 3){
								return 1;
							}
						}
						return 0;
					}
				case 7: 
					lock(this.precommites){
						if (this.precommites.ContainsKey(roundKey)) {
							int cnt = 0;
							foreach (var item in this.precommites[roundKey]){
								if (item.Value.hash == 0){
									cnt ++;
								}
							}
							if(cnt >= this.nodes * 2 / 3 + 1){
								return 1;
							}
						}
						return 0;
					}
				default: 
					return 0;
			}
		}
		
		public int hasDifferentHash(){
			lock(this.preVotes){
				string roundKey = this.consensusHeight+"_"+this.round;
				int  majority = this.nodes * 2 / 3 + 1;
				
				if (this.preVotes.ContainsKey(roundKey)){
					int cnt1 = 0;
					foreach (var item in this.preVotes[roundKey]){
						if (item.Value.hash == 2){
							cnt1 ++;
						}
					}
					if (cnt1 >= 2  &&  cnt1 < 3 ){
						return 1;
					}
					return 0;
				}
				return 0;
			}
		}

		/// Return a deep clone of the hash table
        /// NOTE: this must be a deep clone, shallow clone may lead to strange behaviors.
        /// This method must be overriden
        public override ExpressionValue GetClone() {
            return new ConsensusNode(this.chainId, this.nodeId,this.height,this.consensusHeight, this.round, this.nodes, this.lastProposer, this.proposer,this.preVotes, 
			this.precommites, this.proposal,this.step,this.blocks,this.delayTimes);
        }

        /// Return the compact string representation of the hash table.
        /// This method must be overriden
        /// Smart implementation of this method can reduce the state space and speedup verification 
        public override string ExpressionID {
            get {
            	string returnString = "\nnodeId:";
            	returnString += nodeId.ToString();
				returnString += ", height:";
				returnString += height.ToString();
				returnString += ", consensusHeight:" ;
				returnString += consensusHeight.ToString();
				returnString += ", round:";
				returnString += round.ToString();
				returnString += ", state:";
            	returnString += this.runningState.ToString();
				returnString += ", proposal:";
				if (this.proposal != null) { returnString += this.proposal.ToString();}else{ returnString += "null";}
            	returnString += ", step:";
            	returnString += step.ToString();
				returnString += ", delays: [";
				foreach (var item in this.delayTimes){
					returnString += "[";
					returnString += item.Key + ":";
					returnString += "[";
					foreach (var delay in item.Value){
						returnString += "[";
						returnString += delay.Key + ":" + delay.Value + "],";
					}
				}

				returnString += "]";
				returnString += ", preVotes: [";
				foreach (string hrKey in this.preVotes.Keys)
				{
					returnString += "key: " + hrKey + " value: [" ;
					foreach (int it in this.preVotes[hrKey].Keys)
					{
						returnString += it + " : " + this.preVotes[hrKey][it].ToString();
					}
					returnString += "], ";

				}
				returnString += "]";
				returnString += ", precommites: [";
				foreach (string hrKey in this.precommites.Keys){
					returnString += "key: " + hrKey + "value: ";
					foreach (int it in this.precommites[hrKey].Keys)
					{
						returnString += it+ " : " + this.precommites[hrKey][it].ToString();
					}
					returnString += "], ";
				}
				returnString += "]";
                return returnString;
               }
        }
        
        /// Return the string representation of the hash table.
        /// This method must be overriden
        public override string ToString() {
            return "[" + ExpressionID + "]";
        }
	}
	
	// 区块
	public class Block : ExpressionValue{
		public int	 		chainId;
		public int 			blockType; // 区块类型： 0：NORMAL_BLOCK， 1：CONFIG_BLOCK， 2：CONTRACT_MGR_BLOCK， 4：HAS_COINBASE
		public int 			blockHeight;
		public int 			blockHash;
		public int 			proposer;
		public int		 	sign;
		public long			blockTimestamp;
		public List<Transaction> txs;
		public Block() {
			this.blockHeight = -1; 
		}
		
		public Block(int chainId, int blockType, int height, int blockHash, int proposer, int sign, long blocktime) {
			this.chainId = chainId;
			this.blockType = blockType;
			this.blockHeight = height;
			this.blockHash = blockHash;
			this.proposer  = proposer;
			this.sign = sign;
			this.blockTimestamp = blocktime;
			//this.txs = txs;
		}
		
		public int getProposer() {
			return this.proposer;
		}
		
		public int getHeight(){
			return this.blockHeight;
		}
		
		public override ExpressionValue GetClone() {
			return new Block(this.chainId,this.blockType,this.blockHeight,this.blockHash,this.proposer,this.sign,this.blockTimestamp);
		}
		
		public override string ExpressionID {
			get {
				string id = "chainId: " + this.chainId +",";
				id += "blockType: " + this.blockType +",";
				id += "blockHeight: " +this.blockHeight +",";
				id += "blockHash: " +this.blockHash +",";
				id += "proposer: " +this.proposer +",";
				id += "sign: " + this.sign +",";
				id += "timestamp: " + this.blockTimestamp;
				
				return id;
			}
		}
		
		public override string ToString() {
			return "["+ExpressionID+"]";
		}
	}
	
	// 交易信息
	public class Transaction : ExpressionValue{
		
		public int 	chainId;
		public int 	txId;
		public int 		txType; // INVOKE_CONTRACT:0, QUERY_CONTRACT:1, SUBSCRIBE:2, ARCHIVE:3 
		public long 	txTimestamp;
		public long		expirationTime;
		//public string	sender;
		//public string 	senderSign;
		
		public Transaction(){ }
	
		public override string ExpressionID {
			get {
				return "\n";
			}
		}
		
		public override string ToString(){
			return ExpressionID;
		}
		
		public override ExpressionValue GetClone(){
			return new Transaction();
		}
	}

	// 提议信息
	public class Proposal : ExpressionValue {
		public int height;

		public int round;

		public int proposer;	// 提议者

		public int sign; 		// 提议者签名

		public List<int> qc;		// 收集到的投票列表
		public Block block;			// 提议的区块

		public Proposal() {
			this.qc = new List<int>();
			this.block = new Block();
		}

		public Proposal(int height, int round, int proposer, int sign, List<int> qc,Block block){
			this.height = height;
			this.round  = round;
			this.proposer = proposer;
			this.sign = sign;
			this.qc = qc;
			this.block = block;
		}

		public int getHeight(){
			return this.height;
		}

		public int getRound(){
			return this.round;
		}

		public int getProposer(){
			return this.proposer;
		}

		public int getSign(){
			return this.sign;
		}
		public int sizeQc(){
			return this.qc.Count;
		}

		public override string ExpressionID {
			get {
				string str = "";
				str += "height: " + this.height + ",";
				str += "round: " + this.round + ",";
				str += "proposer: " + this.proposer +",";
				str += "sign: " + this.sign + ",";
				str += "block: " + this.block;

				return str;
			}
		}

		public override string ToString() {
			return ExpressionID;
		}

		public override ExpressionValue GetClone() {
			return new Proposal(this.height, this.round, this.proposer, this.sign, this.qc,this.block);
		}
	}

	// 投票信息
	public class Vote : ExpressionValue {
		public int height;	

		public int round;	

		public int type;  // 2: prevote  3: precommit

		public int voter; // 投票者节点ID

		public int sign; // 投票者签名

		public int hash; // 区块hash

		public Vote() {}

		public Vote(int height, int round, int type, int voter, int sign, int hash) {
			this.height = height;
			this.round = round;
			this.type  = type;
			this.voter = voter;
			this.sign  = sign;
			this.hash = hash;
		}

		public override string ExpressionID {
			get{
				string str = "\n[";
				str += "height: " + this.height+",";
				str += "round: " + this.round+",";
				str += "type: " + this.type +",";
				str += "voter: " + this.voter + ",";
				str += "sign: " + this.sign+",";
				str += "hash: " + this.hash+"]";

				return str;
			}
		}

		public int getHeight() {
			return this.height;
		}

		public override string ToString() {
			return  ExpressionID;
		}

		public override ExpressionValue GetClone(){
			return new Vote(this.height, this.round, this.type, this.voter, this.sign,this.hash);
		}
	}

	// 投票列表
	public class VoteList : ExpressionValue {
		public Dictionary<string, Vote> votes;

		public VoteList() {
				this.votes = new Dictionary<string, Vote>();
		}

		public VoteList(int size) {
				this.votes = new Dictionary<string, Vote>(size);
		}

		public VoteList(Dictionary<string, Vote> voteDic){
				this.votes = voteDic;
		}

		public void clearVotes() {
			lock (this.votes) {
				if (this.votes.Count > 0){
					this.votes.Clear();
				}
			}
		}

		public void addVote(int nodeId,int height, int round, Vote v) {
			lock (this.votes) {
				var key = nodeId + "_"+height+"_"+round;
				this.votes[key] = v;
			}
		}

		public Vote getByKey(int nodeId, int height, int round) {
			var key = nodeId + "_"+height+"_"+round;
			lock (this.votes) {
				
				if (this.votes.ContainsKey(key)) {
					return this.votes[key];
				} else {
					//throw PAT Runtime exception
					// throw new RuntimeException("key "+key+" has not in the dictionary.");
					return null;
				}
			}
		}

		public void remove(int nodeId, int height, int round) {
			var key = nodeId + "_"+height+"_"+round;
			lock (this.votes) {
				if (this.votes.ContainsKey(key)) {
					this.votes.Remove(key);
				} 
			}
		}
		public override string ExpressionID {
			get{
				string str = "{";
				foreach (string k in this.votes.Keys) {
					str += "key : " + k + ", value: " +this.votes[k]+"\n";
				}
				str += "}";
				return str;
			}
		}

		public override string ToString() {
			return ExpressionID;
		}

		public override ExpressionValue GetClone() {
			return new VoteList(this.votes);
		}
	}

	// 区块列表
	public class BlockList : ExpressionValue {
		public Dictionary<int, Block> blocks;

		public BlockList() {
				this.blocks = new Dictionary<int, Block>();
		}

		public BlockList(int size) {
				this.blocks = new Dictionary<int, Block>(size);
		}

		public BlockList(Dictionary<int, Block> blockDic){
				this.blocks = blockDic;
		}

		public void clearBlocks() {
			lock (this.blocks) {
				if (this.blocks.Count > 0){
					this.blocks.Clear();
				}
			}
		}

		public void addBlock(int key, Block v) {
			lock (this.blocks) {
				this.blocks[key] = v;
			}
		}

		public Block getByKey(int key) {

			lock (this.blocks) {
				if (this.blocks.ContainsKey(key)) {
					return this.blocks[key];
				} else {
					//throw PAT Runtime exception
					throw new RuntimeException("key has not in the dictionary.");
				}
			}
		}

		public void remove(int key) {
			lock (this.blocks) {
				if (this.blocks.ContainsKey(key)) {
					this.blocks.Remove(key);
				} else {
					//throw PAT Runtime exception
					throw new RuntimeException("key has not in the dictionary.");
				}
			}
		}
		public override string ExpressionID {
			get{
				string str = "{";
				foreach (int k in this.blocks.Keys) {
					str += "key : " + k + ", value: " +this.blocks[k]+"\n";
				}
				str += "}";
				return str;
			}
		}

		public override string ToString() {
			return ExpressionID;
		}

		public override ExpressionValue GetClone() {
			return new BlockList(this.blocks);
		}
	}


	// 提案列表
	public class ProposalList : ExpressionValue {
		public Dictionary<string, Proposal> proposals;

		public ProposalList() {
				this.proposals = new Dictionary<string, Proposal>();
		}

		public ProposalList(int size) {
				this.proposals = new Dictionary<string, Proposal>(size);
		}

		public ProposalList(Dictionary<string, Proposal> proposalDic){
				this.proposals = proposalDic;
		}

		public void clearProposals() {
			lock (this.proposals) {
				if (this.proposals.Count > 0){
					this.proposals.Clear();
				}
			}
		}

		public void addProposal(int nodeId, int height, int round,  Proposal v) {
			var key = height + "_" + round+"_"+nodeId;

			lock (this.proposals) {
				this.proposals[key] = v;
			}
		}

		public Proposal getProposal(int nodeId, int height, int round) {
			var key = height + "_" + round+"_"+nodeId;
			lock (this.proposals) {
				if (this.proposals.ContainsKey(key)) {
					return this.proposals[key];
				} else {
					//throw PAT Runtime exception
					// throw new RuntimeException("key has not in the dictionary.");
					return null;
				}
			}
		}

		public override string ExpressionID {
			get{ return this.ToString(); }
		}

        public override string ToString() {
            string str = "{";
			foreach (string k in this.proposals.Keys) {
				str += "key : " + k + ", value: " +this.proposals[k]+"\n";
			}
			str += "}";
			return str;
        }

		public override ExpressionValue GetClone() {
			return new ProposalList(this.proposals);
		}
	}
	

	public class NetMsg<T> : ExpressionValue{
        public int msgType {get; set;}
        public T msgData {get; set;}

        public NetMsg(int msgType, T msgData){
            this.msgType = msgType;
            this.msgData = msgData;
        }

        public int getMsgType(){
            return msgType;
        }

        public T getMsgData(){
            return msgData;
        }

        public override string ToString(){
            return "msgType: "+ msgType + ", msgData:"+ msgData;
        }

        public override string ExpressionID {
            get { return "NetMsg"; }
        }

        public override ExpressionValue GetClone() {
            return new NetMsg<T>(msgType, msgData);
        }
    }



	//静态方法
	public class Utils  {
		public static int NextProposer(int last, int height, int round, int cnt){
			int proposerOffset = last;
			if (height % cnt == 0 ) {
				proposerOffset++;
			}
			int roundOffset = round % cnt;
			int currentProposer = (proposerOffset + roundOffset) % cnt;
			return currentProposer;
		}

		public static int MsgType(object msg){
			if(msg == null){
				return 0;
			}
			if(msg is Proposal){
				return 1;
			}else if(msg is Vote){
				Vote v = (Vote)msg;
				return v.type;
			}
			return 0;
		}
	}
}

