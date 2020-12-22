using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes.FAHPayloadInterpeters
{
    public class FAHRPPDeviceRead : FAHReadablePayloadPacket
    {
        public enum IndividualDeviceReadActions : byte
        {
            fSuccess = 0x00,
            fMInscs = 0x02,
            ReadOutputs = 0x03,
            ReadParameters = 0x04,

            //Manual set
            DeviceInfo = 0xA0,
            OperationalStatus = 0xA1,
            DeviceParameters = 0xA2,
            ChannelInfo = 0xA3,
        }

        public IndividualDeviceReadActions Action;
        public uint bitErrors;
        public uint spikeErrors;
        public uint parityErrors;
        public uint operationTime;
        public uint deviceReboots;
        public byte busVoltage_NEED_TO_INTERPET;
        public byte Channel;
        public IndividualDeviceReadActions fprop_cmd;
        public byte fprop_objidx;
        public byte fprop_propid;
        public byte[] ByteData;

        public FAHRPPDeviceRead(KNXPayload payload)
        {
            fprop_cmd = (IndividualDeviceReadActions)payload.ByteData[3];
            fprop_objidx = payload.ByteData[1];
            fprop_propid = payload.ByteData[2];
            addAccountedBytes(0, 4);

            if (fprop_cmd == IndividualDeviceReadActions.fSuccess && fprop_objidx == 0 && fprop_propid == 4)
            {
                //ReadValue0x00 ch000 4
                // ID  0        1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22      23      24      25      26      27      28      29      30      31      32      33      34      35      36      37      38      39      40
                // HEX 0xC9     0x00    0x04    0x00    0x01    0x00    0x3B	0xFF	0xFF	0xFF	0xFF	0x07	0x14	0xC8	0x00	0x0C	0x01	0x00	0x49	0x00	0x00	0x00	0x05	0x00	0x4A	0x00	0x00	0x00	0x09	0x00	0x4B	0x00	0x00	0x00	0x06	0x00	0x4C	0x00	0x00	0x00	0x0A
                // DEC *        *       *       *       1       0		59		255		255		255		255		7		20		200		0		12		1		0		73		0		0		0		5		0		74		0		0		0		9		0		75		0		0		0		6		0		76		0		0		0		10
                //                                      SWVER?  SWVER?  PARAMID MATCHCODE       MATCHCODE       BIT?    DPT     DPT     PARMID  PARMID  Value   optionNameID    MASK--------------------MASK    optionNameID    MASK--------------------MASK    optionNameID    MASK--------------------MASK    optionNameID    MASK--------------------MASK
                //
                // BIT? --> wizardOnly="false" deviceChannelSelector="false" channelSelector="true" writable="true" visible="true"
                //
                /*                  
                 <parameters>
                    <parameter nameId="003B" i="pm0000" optional="false" dependencyId="FFFF" wizardOnly="false" deviceChannelSelector="false" channelSelector="true" writable="true" visible="true" accessLevel="Enduser" parameterId="000C" matchCode="FFFFFFFF" dpt="14C8">
                    <valueEnum>
                        <option nameId="0049" mask="00000005" isDefault="true" key="1"/>
                        <option nameId="004A" mask="00000009" isDefault="false" key="2"/>
                        <option nameId="004B" mask="00000006" isDefault="false" key="3"/>
                        <option nameId="004C" mask="0000000A" isDefault="false" key="4"/>
                    </valueEnum>
                    <value>1</value>
                </parameter>
                */
                //
                fprop_cmd = IndividualDeviceReadActions.DeviceParameters;
                addAccountedBytes(6, 35);
            }
            else if (fprop_cmd == IndividualDeviceReadActions.fSuccess && fprop_objidx != 0 && fprop_propid == 1)
            {
                fprop_cmd = IndividualDeviceReadActions.ChannelInfo;
                addAccountedBytes(3, 1);
                addAccountedBytes(4, 8);
                addAccountedBytes(15, 2);
                //ReadValue0x00 ch0XX 1
                /*
                        0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16  
                 HEX:   0xC9	0x01	0x01	0x00	0x00	0x80	0x00	0x43	0x00	0x00	0x00	0x01	0x00	0x00	0x00	0x01	0x55   //Moredata
                 HEX:   0xC9	0x02	0x01	0x00	0x00	0x40	0x00	0x45	0x00	0x00	0x00	0x02	0x00	0x00	0x00	0x01	0x2A   //Done
                 HEX:   0xC9	0x03	0x01	0x00	0x00	0x60	0x00	0x46	0x00	0x00	0x00	0x02	0x00	0x00	0x00	0x01	0x2A
                 HEX:   0xC9	0x04	0x01	0x00	0x00	0x80	0x00	0x44	0x00	0x00	0x00	0x04	0x00	0x00	0x00	0x01	0x55
                 HEX:   0xC9	0x05	0x01	0x00	0x00	0x40	0x00	0x47	0x00	0x00	0x00	0x08	0x00	0x00	0x00	0x01	0x2A
                 HEX:   0xC9	0x06	0x01	0x00	0x00	0x60	0x00	0x48	0x00	0x00	0x00	0x08	0x00	0x00	0x00	0x01	0x2A
                 HEX:   0xC9	0x07	0x01	0x00	0x02	0x60	0x00	0x4F	0xFF	0xFF	0xFF	0xFF	0x00	0x00	0x00	0x00	0xAA    //FunctionID 12 ??
                 DESC:     *       *       *    fprop   CHANID  CHANID  ChanNID ChanNID MASK--------------------MASK                            COMBIND MoreDataMask?
                                                retval
	            */
            }
            else if ((fprop_cmd == IndividualDeviceReadActions.fMInscs || fprop_cmd == IndividualDeviceReadActions.fSuccess) && (fprop_objidx > 2))
            {
                    /*
                    //Inputs
                    ReadValue0x02   ch007	2	0xC9	0x07	0x02	0x02	0x01	0x00	0x01	0x00	0x0B	0x00	0x00	0x00	0x01	0x01	0x01	0x20	0x00	0x00
                    ReadValue0x02   ch007	2	0xC9	0x07	0x02	0x02	0x02	0x00	0x10	0x00	0x10	0x00	0x00	0x00	0x01	0x03	0x07	0x20	0x00	0x00
                    ReadValue0x02	ch007	2	0xC9	0x07	0x02	0x02	0x03	0x00	0x11	0x00	0x11	0x00	0x00	0x00	0x01	0x05	0x01	0x20	0x00	0x00
                    ReadValue0x02	ch007	2	0xC9	0x07	0x02	0x02	0x04	0x00	0x02	0x00	0x0C	0x00	0x00	0x00	0x01	0x01	0x0A	0x20	0x00	0x00
                    ReadValue0x02	ch007	2	0xC9	0x07	0x02	0x02	0x05	0x00	0x03	0x00	0x0D	0x00	0x00	0x00	0x01	0x02	0x01	0x20	0x00	0x00
                    ReadValue0x02	ch007	2	0xC9	0x07	0x02	0x02	0x06	0x00	0x04	0x00	0x0E	0x00	0x00	0x00	0x01	0x12	0x01	0x20	0x00	0x00
                    ReadValue0x02	ch007	2	0xC9	0x07	0x02	0x02	0x07	0x00	0x12	0x00	0x12	0x00	0x00	0x00	0x01	0x01	0x02	0x10	0x00	0x04
                    ReadValue0x00	ch007	2	0xC9	0x07	0x02	0x00	0x08	0x00	0x06	0x01	0xF6	0x00	0x00	0x00	0x01	0x01	0x0A	0x20	0x00	0x00
                                                *	    *	    *	    fprop	Datpoint
                                                                        retval	
                    //Outputs
                    ReadValue0x02	ch007	3	0xC9	0x07	0x03	0x02	0x01	0x01	0x00	0x00	0x0F	0x00	0x00	0x00	0x01	0x01	0x01	0x01	0x00	0x08
                    ReadValue0x02	ch007	3	0xC9	0x07	0x03	0x02	0x02	0x01	0x10	0x01	0x20	0x00	0x00	0x00	0x01	0x05	0x01	0x01	0x00	0x08
                    ReadValue0x02	ch007	3	0xC9	0x07	0x03	0x02	0x03	0x01	0x11	0x00	0x14	0x00	0x00	0x00	0x01	0x15	0x03	0x01	0x00	0x08
                    ReadValue0x00	ch007	3	0xC9	0x07	0x03	0x00	0x04	0x01	0x01	0x02	0x04	0x00	0x00	0x00	0x01	0x14	0x64	0x01	0x00	0x08
                                                *	    *	    *	    fprop	Datpoint
                                                                        retval	
                    //Parameters
                    ReadValue0x02	ch007	4	0xC9	0x07	0x04	0x02	0x01	0x00	0xFC	0x00	0x00	0x00	0x01	0x0B	0x14	0x64	0x00	0x13	0x03	0x00	0xFB	0x01	0x8A	0x00	0xFA
                    ReadValue0x02	ch007	4	0xC9	0x07	0x04	0x02	0x02	0x01	0x8B	0x00	0x00	0x00	0x01	0x03	0x05	0x01	0x00	0x04	0x00	0x00	0x00	0x01	0x00	0x00	0x00	0x01	0x00	0x00	0x00	0x32	0x00	0x00	0x00	0x01	0x40	0x23	0x33	0x33
                    ReadValue0x02	ch007	4	0xC9	0x07	0x04	0x02	0x03	0x01	0xF4	0x00	0x00	0x00	0x01	0x03	0x05	0x01	0x00	0x05	0x00	0x00	0x00	0x64	0x00	0x00	0x00	0x0A	0x00	0x00	0x00	0x64	0x00	0x00	0x00	0x01	0x40	0x23	0x33	0x33
                    ReadValue0x02	ch007	4	0xC9	0x07	0x04	0x02	0x04	0x01	0xF5	0x00	0x00	0x00	0x01	0x03	0x05	0x01	0x00	0x12	0x00	0x00	0x00	0x64	0x00	0x00	0x00	0x0A	0x00	0x00	0x00	0x64	0x00	0x00	0x00	0x01	0x40	0x23	0x33	0x33
                    ReadValue0x00	ch007	4	0xC9	0x07	0x04	0x00	0x05	0x01	0x6D	0x00	0x00	0x00	0x01	0x03	0x07	0x05	0xFF	0xFF	0x00	0x00	0x00	0x3C	0x00	0x00	0x00	0x1E	0x00	0x00	0x07	0x08	0x00	0x00	0x00	0x0A	0x3F	0x80	0x00	0x00
                                                *	    *	    *	    fprop	Datpoint
                                                                        retval	
                    */
                    //Console.WriteLine("Todo!");
            }
            else if (fprop_cmd == IndividualDeviceReadActions.fSuccess && fprop_objidx == 0 && fprop_propid == 1)
            {
                //ReadValue0x00 ch000 1
                if (payload.ByteData[4] == 0xFF)
                {
                    fprop_cmd = IndividualDeviceReadActions.DeviceInfo;
                    addAccountedBytes(4, 4);
                    addAccountedBytes(11, 4);
                    addAccountedBytes(15, 4);
                    addAccountedBytes(21, 2);
                    //
                    //     0        1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22  
                    // HEX 0xC9	    0x00	0x01	0x00	0xFF	0xFD	0xFE	0xFF	0x15	0x56	0x37	0x00	0x7A	0x3C	0xF9	0x00	0x00	0x05	0x56	0x02	0x00	0x01	0x00
                    // DEC 201		0		1		0		255		253		254		255		21		86		55		0		122		60		249		0		0		5		86		2		0		1		0
                    //
                    // HEX 0xC9	    0x00	0x01	0x00	0xFF	0xEB	0xFE	0xFF	0x08	0x00	0x37	0x00	0x6A	0xF6	0xD1	0x00	0x00	0x02	0x7E
                    // DEC 201		0		1		0		255		235		254		255		8		0		55		0		106		246		209		0		0		2		126
                    // NAME         *       *       *       NAMEID	NAMEID	FUNCTID	FUNCTID				            COMPILER------------COMPILER	BUILD------------------BUILD
                }
                else if (payload.ByteData[4] == 0x01)
                {
                    Console.WriteLine("Todo!");
                }
                else if (payload.ByteData[4] == 0x07)
                {
                    //      0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22      23      24      25      26      27      28      29      30      31
                    // HEX  0xC9	0x00	0x01	0x00	0x07	0x78	0x00	0x00	0x04	0x31	0x00	0x03	0x00	0x00	0x00	0xBF	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00
                    // DESC *       *       *       *       CHANCNT Voltage OperationTime   OperationTime   devicereboots                                                   biterrors                                       parityerrors    biterrors
                    //ReadValue0x00 ch000 4
                    fprop_cmd = IndividualDeviceReadActions.OperationalStatus;


                    if (!(payload.Length >= 5)) return;
                    busVoltage_NEED_TO_INTERPET = payload.ByteData[5];
                    addAccountedBytes(5, 1);

                    if (!(payload.Length >= 6)) return;
                    operationTime = KNXDataConversion.knx_to_uint32(payload.GetBytes(6, 4));
                    addAccountedBytes(6, 4);

                    if (!(payload.Length >= 12)) return;
                    deviceReboots = KNXDataConversion.knx_to_uint16(payload.GetBytes(10, 2));
                    addAccountedBytes(10, 2);

                    if (!(payload.Length >= 20)) return;
                    bitErrors = KNXDataConversion.knx_to_uint16(payload.GetBytes(18, 2));
                    addAccountedBytes(18, 2);

                    if (!(payload.Length >= 26)) return;
                    parityErrors = KNXDataConversion.knx_to_uint16(payload.GetBytes(24, 2));
                    addAccountedBytes(24, 2);

                    if (!(payload.Length >= 28)) return;
                    spikeErrors = KNXDataConversion.knx_to_uint16(payload.GetBytes(26, 2));
                    addAccountedBytes(26, 2);
                }
            }

                /*
                //Action = (FAHRPPDeviceRead.IndividualDeviceReadActions)payload.ByteData[2];
                //addAccountedBytes(0, 3);
                //Detection Method unsure, might be needed to check on lenght of packet or something
                //Action = (FAHRPPDeviceRead.IndividualDeviceReadActions)payload.ByteData[4];
                if (Action == FAHRPPDeviceRead.IndividualDeviceReadActions.DeviceOperationalStatus)
                {                

                    if (!(payload.Length >= 5)) return;
                    busVoltage_NEED_TO_INTERPET = payload.ByteData[5];
                    addAccountedBytes(5, 1);

                    if (!(payload.Length >= 6)) return;
                    operationTime = KNXDataConversion.knx_to_uint32(payload.GetBytes(6, 4));
                    addAccountedBytes(6, 4);

                    if (!(payload.Length >= 12)) return;
                    deviceReboots = KNXDataConversion.knx_to_uint16(payload.GetBytes(10, 2));
                    addAccountedBytes(10, 2);

                    if (!(payload.Length >= 20)) return;
                    bitErrors = KNXDataConversion.knx_to_uint16(payload.GetBytes(18, 2));
                    addAccountedBytes(18, 2);

                    if (!(payload.Length >= 26)) return;
                    parityErrors = KNXDataConversion.knx_to_uint16(payload.GetBytes(24, 2));
                    addAccountedBytes(24, 2);

                    if (!(payload.Length >= 28)) return;
                    spikeErrors = KNXDataConversion.knx_to_uint16(payload.GetBytes(26, 2));
                    addAccountedBytes(26, 2);
                }
                else if (Action == IndividualDeviceReadActions.DeviceConfigurationInformation)
                {

                }
                else if (Action == FAHRPPDeviceRead.IndividualDeviceReadActions.RequestDeviceInformation)
                {
                    //Todo
                    Channel = payload[1];
                    if (Channel == 0)
                    {
                        addAccountedBytes(0, 2);
                        addAccountedBytes(4, 4);
                        addAccountedBytes(11, 4);
                        addAccountedBytes(15, 3);
                         //
                         // 0xC9	    0x00	0x01	0x00	0xFF	0xFD	0xFE	0xFF	0x15	0x56	0x37	0x00	0x7A	0x3C	0xF9	0x00	0x00	0x05	0x56	0x02	0x00	0x01	0x00
                         //                                     NAMEID	NAMEID	FUNCTID	FUNCTID				            COMPILER------------COMPILER	BUILD----------BUILD				

                    }
                    else
                    {
                        var w = 0;
                    }

                }
                else
                {
                    var x = 0;
                }*/
            }

        protected override string PrintOut()
        {
            if (ByteData != null)
            {
                string hex = BitConverter.ToString(ByteData).Replace('-', ' ');
                return string.Format("IDR: {0} ch{1:D3} {2} [{3}] ", fprop_cmd, fprop_objidx, fprop_propid, hex);
            }
            else
            {
                return string.Format("IDR: {0} ch{1:D3} {2} ", fprop_cmd, fprop_objidx, fprop_propid);
            }
        }
    }
}
