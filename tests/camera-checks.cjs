// Executes the actual CameraService with simulated browser media and SignalR APIs.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const ts = require('../src/frontend/web/node_modules/typescript');
const source = fs.readFileSync(path.join(__dirname,'../src/frontend/web/src/app/core/services/camera.service.ts'),'utf8');
const compiled = ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS,experimentalDecorators:true}}).outputText;
const hubs=[]; const pcs=[]; let capture;
class Hub {
  state='Disconnected'; handlers={}; calls=[];
  on(name,fn){this.handlers[name]=fn;}
  onreconnecting(fn){this.reconnecting=fn;}
  onreconnected(fn){this.reconnected=fn;}
  onclose(fn){this.closed=fn;}
  async start(){this.state='Connected';}
  async stop(){this.state='Disconnected';this.closed?.();}
  async invoke(name,...args){this.calls.push([name,...args]);if(name==='JoinCamera')this.handlers['camera-peers']([2],1);}
}
class Builder {
  withUrl(){return this;} withAutomaticReconnect(){return this;}
  build(){const h=new Hub();hubs.push(h);return h;}
}
class PC {
  signalingState='stable'; remoteDescription=null; senders=[]; transceivers=[]; addedIce=[];
  constructor(){pcs.push(this);}
  addTrack(track,stream){
    const sender={track, async replaceTrack(value){this.track=value;}};
    this.senders.push(sender);this.transceivers.push({sender,receiver:{track:{kind:track.kind}},direction:'sendrecv'});
  }
  getSenders(){return this.senders;} getTransceivers(){return this.transceivers;}
  async setLocalDescription(){this.localDescription={type:this.signalingState==='have-remote-offer'?'answer':'offer',sdp:'test'};this.signalingState=this.localDescription.type==='offer'?'have-local-offer':'stable';}
  async setRemoteDescription(value){this.remoteDescription=value;this.signalingState=value.type==='offer'?'have-remote-offer':'stable';}
  async addIceCandidate(ice){assert.ok(this.remoteDescription);this.addedIce.push(ice);}
  close(){this.signalingState='closed';}
}
function signal(value){const f=()=>value;f.set=v=>value=v;f.update=fn=>value=fn(value);return f;}
const context={exports:{}, console, RTCPeerConnection:PC, MediaStream:class {}, navigator:{mediaDevices:{getUserMedia:args=>capture(args)}},require:name=>{
  if(name==='@angular/core')return{Injectable:()=>x=>x,signal};
  if(name==='@microsoft/signalr')return{HubConnectionBuilder:Builder,HubConnectionState:{Connected:'Connected'}};
  throw new Error(name);
}};
vm.runInNewContext(compiled,context);
const Camera=context.exports.CameraService;
const tick=()=>new Promise(resolve=>setImmediate(resolve));
(async()=>{
  const c=new Camera();let captures=0;let stops=0;
  const track={kind:'video',stop(){stops++;}};const stream={getTracks:()=>[track]};
  capture=async args=>{assert.equal(args.audio,false);assert.equal(args.video,true);captures++;return stream;};
  await Promise.all([c.connect(),c.connect()]);
  assert.equal(hubs.length,1);assert.equal(captures,0);
  await Promise.all([c.enable(),c.enable()]);await tick();
  assert.equal(captures,1);assert.equal(c.enabled(),true);assert.equal(pcs.length,1);
  const incoming={name:'incoming'};
  pcs[0].ontrack({streams:[incoming]});assert.equal(c.feeds().length,1);
  hubs[0].handlers['camera-signal'](2,{cameraOff:true});await tick();assert.equal(c.feeds().length,0);
  hubs[0].handlers['camera-signal'](2,{cameraOn:true});await tick();assert.equal(c.feeds()[0].stream,incoming);
  c.disable();await tick();
  assert.equal(stops,1);assert.equal(c.localStream(),null);assert.equal(c.feeds()[0].stream,incoming);
  assert.equal(pcs[0].senders[0].track,null);assert.ok(hubs[0].calls.some(call=>call[2]?.cameraOff));
  await c.enable();await tick();
  assert.equal(pcs[0].senders.length,1);assert.equal(pcs[0].senders[0].track,track);
  hubs[0].handlers['camera-signal'](3,{ice:{candidate:'queued'}});await tick();
  const queued=pcs[1];assert.equal(queued.addedIce.length,0);
  hubs[0].handlers['camera-signal'](3,{description:{type:'offer',sdp:'test'}});await tick();
  assert.equal(queued.addedIce.length,1);
  hubs[0].reconnecting();assert.equal(c.feeds().length,0);
  await hubs[0].reconnected();assert.equal(hubs[0].calls.filter(x=>x[0]==='JoinCamera').length,2);
  await c.disconnect();assert.equal(c.enabled(),false);assert.equal(hubs[0].state,'Disconnected');
  const cancelled=new Camera();let release;
  capture=()=>new Promise(resolve=>release=resolve);
  const pending=cancelled.enable();await tick();
  cancelled.disable();release(stream);await pending;
  assert.equal(cancelled.enabled(),false);assert.equal(cancelled.localStream(),null);
  await cancelled.disconnect();
  console.log('Camera checks passed: permission, duplicate clicks, stop/re-enable, incoming video, ICE queue, reconnect, cancellation.');
})().catch(error=>{console.error(error);process.exitCode=1;});
